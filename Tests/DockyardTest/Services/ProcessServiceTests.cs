﻿using System;
using System.IO;
using System.Linq;
using Core.Interfaces;
using Core.Managers;
using Core.Services;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using NUnit.Framework;
using StructureMap;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Moq;
using Data.Interfaces.DataTransferObjects;
using System.Collections.Generic;

using File = System.IO.File;

namespace DockyardTest.Services
{
	[TestFixture]
	[Category("ProcessService")]
	public class ProcessServiceTests : BaseTest
	{
		private IProcess _processService;
		private IDocuSignNotification _docuSignNotificationService;
		private DockyardAccount _userService;
		private string _testUserId = "testuser";
		private string xmlPayloadFullPath;
        DocuSignEventDO docusignEventDO;
        ProcessNodeDO processNodeDO;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			_processService = ObjectFactory.GetInstance<IProcess>();
			_userService = ObjectFactory.GetInstance<DockyardAccount>();
			_docuSignNotificationService = ObjectFactory.GetInstance<IDocuSignNotification>();

			xmlPayloadFullPath = FixtureData.FindXmlPayloadFullPath(Environment.CurrentDirectory);
			if (xmlPayloadFullPath == string.Empty)
				throw new Exception("XML payload file for testing DocuSign notification is not found.");

            docusignEventDO = FixtureData.TestDocuSignEvent1();
            processNodeDO = FixtureData.TestProcessNode2();
		}

		[Test]
		[ExpectedException(typeof (ArgumentException))]
		public void ProcessService_ThrowsIfXmlInvalid()
		{
			_docuSignNotificationService.Process(_testUserId,
				File.ReadAllText(xmlPayloadFullPath.Replace(".xml", "_invalid.xml")));
		}

		[Test]
		public void ProcessService_NotificationReceivedAlertCreated()
		{
            //Arrange 
            //create a test process
		    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
		    {
		        var process = FixtureData.TestProcess1();
		        process.EnvelopeId = "0aa561b8-b4d9-47e0-a615-2367971f876b";
		        process.ProcessState = ProcessState.Executing;
                uow.ProcessTemplateRepository.Add(FixtureData.TestProcessTemplate2());
		        uow.ProcessRepository.Add(process);
		        uow.SaveChanges();
		    }

            //subscribe the events
		    new EventReporter().SubscribeToAlerts();

            //Act
			_docuSignNotificationService.Process(_testUserId, File.ReadAllText(xmlPayloadFullPath));

            //Assert
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				var fact = uow.FactRepository.GetAll().Where(f => f.Activity == "Received").SingleOrDefault();
				Assert.IsNotNull(fact);
			}
		}

		[Test]
		public void ProcessService_CanRetrieveValidProcesses()
		{
			//Arrange 
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
                var processTemplate = FixtureData.TestProcessTemplate2();
                uow.ProcessTemplateRepository.Add(processTemplate);
				foreach (var p in FixtureData.GetProcesses())
				{
					uow.ProcessRepository.Add(p);
				}
				uow.SaveChanges();
			}

			//Act
			var processList = _userService.GetProcessList(_testUserId);

			//Assert
			Assert.AreEqual(2, processList.Count());
		}

		[Test]
		[Ignore("Seems like this test has no sense anymore due to the latest process changes")]
		public void ProcessService_CanCreateProcessProcessingAlert()
		{
			//Arrange 
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				foreach (var p in FixtureData.GetProcesses())
				{
					uow.ProcessRepository.Add(p);
				}
				uow.SaveChanges();
			}

			//Act
			_docuSignNotificationService.Process(_testUserId, File.ReadAllText(xmlPayloadFullPath));

			//Assert
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				var fact = uow.FactRepository.GetAll().Where(f => f.Activity == "Processed");
				Assert.AreEqual(2, fact.Count());
			}
		}

		[Test]
		public void ProcessService_Can_CreateProcess()
		{
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				var envelope = FixtureData.TestEnvelope1();
				var processTemplate = FixtureData.TestProcessTemplate1();

				uow.EnvelopeRepository.Add(envelope);
				uow.ProcessTemplateRepository.Add(processTemplate);
				uow.SaveChanges();

				var process = _processService.Create(processTemplate.Id, envelope.DocusignEnvelopeId);
				Assert.IsNotNull(process);
				Assert.IsTrue(process.Id > 0);
			}
		}

        [Test]
        public void Process_CanAccessProcessNodes()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //Arrange
                var envelope = FixtureData.TestEnvelope1();
                var processTemplate = FixtureData.TestProcessTemplate1();

                uow.EnvelopeRepository.Add(envelope);
                uow.ProcessTemplateRepository.Add(processTemplate);
                uow.SaveChanges();

                //Act
                ProcessDO curProcess = _processService.Create(processTemplate.Id, envelope.DocusignEnvelopeId);

                //Assert
                int expectedProcessNodeCount = uow.ProcessNodeRepository.GetAll().Count();
                int actualprocessNodeCount = curProcess.ProcessNodes.Count;
                Assert.AreEqual(expectedProcessNodeCount, actualprocessNodeCount);
            }
        }

        public void ProcessNode_CanAccessParentProcess()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //Arrange
                var envelope = FixtureData.TestEnvelope1();
                var processTemplate = FixtureData.TestProcessTemplate1();

                uow.EnvelopeRepository.Add(envelope);
                uow.ProcessTemplateRepository.Add(processTemplate);
                uow.SaveChanges();

                //Act
                ProcessDO curProcess = _processService.Create(processTemplate.Id, envelope.DocusignEnvelopeId);

                //Assert
                int expectedProcessId = curProcess.ProcessNodes.First().ParentProcessId;
                int actualprocessId = uow.ProcessNodeRepository.GetByKey(curProcess.ProcessNodes.First().Id).Id;
                Assert.AreEqual(expectedProcessId, actualprocessId);
            }
        }


		[Test]
		[ExpectedException(typeof (ArgumentNullException))]
		public void ProcessService_CanNot_CreateProcessWithIncorrectProcessTemplate()
		{
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				const int incorrectProcessTemplateId = 2;

				var envelope = FixtureData.TestEnvelope1();

				uow.EnvelopeRepository.Add(envelope);
				uow.SaveChanges();
				_processService.Create(incorrectProcessTemplateId, envelope.DocusignEnvelopeId);
			}
		}

		[Test]
        [Ignore("Too broad for a unit test")]
		public void ProcessService_Can_ExecuteWithoutExceptions()
		{
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				var template = FixtureData.TestProcessTemplate1();
				var curEvent = FixtureData.TestDocuSignEvent1();


				uow.ProcessTemplateRepository.Add(template);
				uow.SaveChanges();

				_processService.Launch(template, curEvent);
			}
		}

        [Test]
        [ExpectedException(ExpectedMessage = "ProcessNode.NodeTransitions did not have a key matching the returned transition target from Critera")]
        public void Execute_NoMatchedNodeTransition_ThrowExceptionProcessNodeTransitions()
        {
            docusignEventDO = FixtureData.TestDocuSignEvent1();
            processNodeDO = FixtureData.TestProcessNode3();
            //mock processnode
            var processNodeMock = new Mock<IProcessNode>();
            processNodeMock
                .Setup(c => c.Execute(It.IsAny<List<EnvelopeDataDTO>>(), It.IsAny<ProcessNodeDO>()))
                .Returns("true1");
            ObjectFactory.Configure(cfg => cfg.For<IProcessNode>().Use(processNodeMock.Object));

            _processService = ObjectFactory.GetInstance<IProcess>();

            _processService.Execute(docusignEventDO, processNodeDO);
        }

        [Test]
        public void Execute_MatchedNodeTransition_ProcessNodeNull()
        {
            //mock processnode
            var processNodeMock = new Mock<IProcessNode>();
            processNodeMock
                .Setup(c => c.Execute(It.IsAny<List<EnvelopeDataDTO>>(), It.IsAny<ProcessNodeDO>()))
                .Returns("true");
            ObjectFactory.Configure(cfg => cfg.For<IProcessNode>().Use(processNodeMock.Object));
            //setup the next transition node during lookup key
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ProcessTemplateRepository.Add(FixtureData.TestProcessTemplate2());
                uow.ActionListRepository.Add(FixtureData.TestActionList6());
                uow.SaveChanges();
                
                uow.ProcessRepository.Add(FixtureData.TestProcess1());
                uow.SaveChanges();
                uow.ProcessNodeRepository.Add(FixtureData.TestProcessNode4());
               uow.SaveChanges();
            }
            _processService = ObjectFactory.GetInstance<IProcess>();

            docusignEventDO = FixtureData.TestDocuSignEvent1();
            var processNodeDO = FixtureData.TestProcessNode3();


            _processService.Execute(docusignEventDO, processNodeDO);

            Assert.Pass();//just set to pass because processNodeDo parameter will be set to null(where caller object is unaware) and reaching this line is success
        }
	}
}