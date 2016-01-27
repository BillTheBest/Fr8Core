﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Hub.StructureMap;
using Moq;
using NUnit.Framework;
using StructureMap;
using terminalPapertrail.Actions;
using terminalPapertrail.Interfaces;
using terminalPapertrail.Tests.Infrastructure;
using TerminalBase.Infrastructure;
using Utilities.Logging;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;

namespace terminalPapertrail.Tests.Actions
{
    [TestFixture]
    [Category("terminalPapertrailActions")]
    public class Write_To_LogTests : BaseTest
    {
        private Write_To_Log_v1 _action_under_test;

        public override void SetUp()
        {
            base.SetUp();
            TerminalBootstrapper.ConfigureTest();
            TerminalPapertrailMapBootstrapper.ConfigureDependencies(StructureMapBootStrapper.DependencyType.TEST);

            //setup the rest client
            Mock<IRestfulServiceClient> restClientMock = new Mock<IRestfulServiceClient>(MockBehavior.Default);
            restClientMock.Setup(restClient => restClient.GetAsync<PayloadDTO>(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(Task.FromResult(PreparePayloadDTOWithLogMessages()));
            ObjectFactory.Container.Inject(typeof(IRestfulServiceClient), restClientMock.Object);
            
            _action_under_test = new Write_To_Log_v1();
        }

        [Test]
        public async Task Configure_InitialConfigurationResponse_ShourldReturn_OneConfigControlsCrate()
        {
            //Act
            var result = await _action_under_test.Configure(new ActivityDO());
            ActivityDTO resultActionDTO = Mapper.Map<ActivityDTO>(result);

            //Assert
            var crateStorage = new CrateManager().FromDto(resultActionDTO.CrateStorage);
            Assert.AreEqual(1, crateStorage.Count, "Initial configuration is failed for Write To Log activity in Papertrail");

            var configControlCrates = crateStorage.CratesOfType<StandardConfigurationControlsCM>().ToList();
            Assert.AreEqual(1, configControlCrates.Count, "More than one configuration controls are avaialbe for Write To Log activity");

            var targetUrlControl = configControlCrates.First().Content.Controls[0];
            Assert.IsNotNull(targetUrlControl, "Papertrail target URL control is not configured.");
            Assert.AreEqual("TargetUrlTextBox", targetUrlControl.Name, "Papertrail target URL control is not configured correctly");
        }

        [Test]
        public async Task Configure_FollowUpConfigurationResponse_ShourldReturn_OneConfigControlsCrate()
        {
            //Arrange
            ActivityDO testAction = new ActivityDO();
            await _action_under_test.Configure(testAction);

            //Act
            var result = await _action_under_test.Configure(testAction);
            ActivityDTO resultActionDTO = Mapper.Map<ActivityDTO>(result);

            //Assert
            var crateStorage = new CrateManager().FromDto(resultActionDTO.CrateStorage);
            Assert.AreEqual(1, crateStorage.Count, "Followup configuration is failed for Write To Log activity in Papertrail");

            var configControlCrates = crateStorage.CratesOfType<StandardConfigurationControlsCM>().ToList();
            Assert.AreEqual(1, configControlCrates.Count, "More than one configuration controls are avaialbe for Write To Log activity");

            var targetUrlControl = configControlCrates.First().Content.Controls[0];
            Assert.IsNotNull(targetUrlControl, "Papertrail target URL control is not configured.");
            Assert.AreEqual("TargetUrlTextBox", targetUrlControl.Name, "Papertrail target URL control is not configured correctly");
        }


        [Test]
        public async Task Run_WithOneUpstreamLogMessage_ShouldLogOneTime()
        {
            //Arrange

            //create initial and follow up configuration
            ActivityDO testAction = new ActivityDO();
            await _action_under_test.Configure(testAction);
            await _action_under_test.Configure(testAction);
            
            //Act
            var result = await _action_under_test.Run(testAction, Guid.NewGuid(), null);

            //Assert
            var loggedMessge = new CrateManager().GetStorage(result).CrateContentsOfType<StandardLoggingCM>().Single();
            Assert.IsNotNull(loggedMessge, "Logged message is missing from the payload");
            Assert.AreEqual(1, loggedMessge.Item.Count, "Logged message is missing from the payload");

            Assert.IsTrue(loggedMessge.Item[0].IsLogged, "Log did not happen");

            Mock<IPapertrailLogger> papertrailLogger = Mock.Get(ObjectFactory.GetInstance<IPapertrailLogger>());
            papertrailLogger.Verify( logger => logger.LogToPapertrail(It.IsAny<string>(), It.IsAny<int>(), "Test Log Message"), Times.Exactly(1));
            papertrailLogger.VerifyAll();
        }

        [Test]
        public async Task Run_SecondTimeForSameLog_WithOneUpstreamLogedMessage_LoggedAlready_ShouldLogOnlyOneTime()
        {
            //Arrange

            //create initial and follow up configuration
            ActivityDO testAction = new ActivityDO();
            await _action_under_test.Configure(testAction);
            await _action_under_test.Configure(testAction);

            //log first time
            var result = await _action_under_test.Run(testAction, Guid.NewGuid(), null);

            //Act
            //try to log the same message again
            result = await _action_under_test.Run(testAction, Guid.NewGuid(), null);

            //Assert
            var loggedMessge = new CrateManager().GetStorage(result).CrateContentsOfType<StandardLoggingCM>().Single();
            Assert.IsNotNull(loggedMessge, "Logged message is missing from the payload");
            Assert.AreEqual(1, loggedMessge.Item.Count, "Logged message is missing from the payload");

            Assert.IsTrue(loggedMessge.Item[0].IsLogged, "Log did not happen");

            Mock<IPapertrailLogger> papertrailLogger = Mock.Get(ObjectFactory.GetInstance<IPapertrailLogger>());
            papertrailLogger.Verify(logger => logger.LogToPapertrail(It.IsAny<string>(), It.IsAny<int>(), "Test Log Message"), Times.Exactly(1));
            papertrailLogger.VerifyAll();
        }

        private PayloadDTO PreparePayloadDTOWithLogMessages()
        {
            var curPayload = FixtureData.PayloadDTO1();
            var logMessages = new StandardLoggingCM()
            {
                Item = new List<LogItemDTO>
                {
                    new LogItemDTO
                    {
                        Data = "Test Log Message",
                        IsLogged = false
                    }
                }
            };

            using (var updater = new CrateManager().UpdateStorage(curPayload))
            {
                updater.CrateStorage.Add(Crate.FromContent("Log Messages", logMessages));
            }

            return curPayload;
        }
    }
}