﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Data.Interfaces;
using Data.States;
using Hub.Interfaces;
using Moq;
using NUnit.Framework;
using StructureMap;
using terminalDocuSign.Services;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Data.Entities;

namespace terminalDocuSign.Tests.Services
{
    [TestFixture]
    public class DocuSignRouteTests : BaseTest
    {
        private DocuSignRoute _curDocuSignRoute;

        public override void SetUp()
        {
            base.SetUp();
            _curDocuSignRoute = new DocuSignRoute();
        }

        [Test, Category("DocuSignRoute_CreteRoute")]
        public async Task CreateRoute_InitialAuthenticationSuccessful_MonitorAllDocuSignEvents_RouteCreatedWithTwoActions()
        {
            //Arrange
            SetupForAutomaticRoute();

            //Act
            await _curDocuSignRoute.CreateRoute_MonitorAllDocuSignEvents(FixtureData.TestDeveloperAccount().Id);

            //Assert
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                Assert.AreEqual(1, uow.RouteRepository.GetAll().Count(), "Automatic route is not created");

                var automaticRoute = uow.RouteRepository.GetQuery().First();

                Assert.AreEqual("MonitorAllDocuSignEvents", automaticRoute.Name, "Automatic route name is wrong");
                Assert.AreEqual(1, automaticRoute.Subroutes.Count(), "Automatic subroute is not created");
                Assert.AreEqual(2, automaticRoute.Subroutes.First().ChildNodes.Count, "Automatic route does not contain required actions");
            }
        }

        [Test, Category("DocuSignRoute_CreteRoute")]
        public async Task CreateRoute_SameUserAuthentication_MonitorAllDocuSignEvents_RouteCreatedOnlyOnce()
        {
            //Arrange
            SetupForAutomaticRoute();
            //call for first time auth successfull
            await _curDocuSignRoute.CreateRoute_MonitorAllDocuSignEvents(FixtureData.TestDeveloperAccount().Id);

            //Act
            //if we call second time, the route should not be created again.
            await _curDocuSignRoute.CreateRoute_MonitorAllDocuSignEvents(FixtureData.TestDeveloperAccount().Id);

            //Assert
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                Assert.IsFalse(uow.RouteRepository.GetAll().Count() > 1, "Automatic route is created in following authentication success");
            }
        }

        private void SetupForAutomaticRoute()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //Create a test account
                var testFr8Account = FixtureData.TestDeveloperAccount();
                testFr8Account.Email = "test@email.com";
                uow.UserRepository.Add(testFr8Account);

                //create required activities
                var recordDocuSignActivityTemplate = FixtureData.TestActivityTemplateDO_RecordDocuSignEvents();
                var storeMTDataActivityTemplate = FixtureData.TestActivityTemplateDO_StoreMTData();

                recordDocuSignActivityTemplate.AuthenticationType = storeMTDataActivityTemplate.AuthenticationType = AuthenticationType.None;

                uow.TerminalRepository.Add(recordDocuSignActivityTemplate.Terminal);
                uow.TerminalRepository.Add(storeMTDataActivityTemplate.Terminal);

                uow.ActivityTemplateRepository.Add(recordDocuSignActivityTemplate);
                uow.ActivityTemplateRepository.Add(storeMTDataActivityTemplate);

                uow.SaveChanges();

                //create and mock required acitons
                var recordDocuSignAction = FixtureData.TestAction1();
                var storeMtDataAction = FixtureData.TestAction2();

                //setup Action Service
                Mock<IAction> _actionMock = new Mock<IAction>(MockBehavior.Default);

                _actionMock.Setup(
                    a => a.CreateAndConfigure(It.IsAny<IUnitOfWork>(), It.IsAny<string>(), It.IsAny<int>(),
                        "Record_DocuSign_Events", It.IsAny<string>(), It.IsAny<Guid>(), false)).Callback(() =>
                        {
                            using (var uow1 = ObjectFactory.GetInstance<IUnitOfWork>())
                            {
                                uow1.ActionRepository.Add(recordDocuSignAction);

                                var subRoute = uow1.SubrouteRepository.GetQuery().Single();
                                subRoute.ChildNodes.Add(recordDocuSignAction);

                                uow1.SaveChanges();
                            }
                        }).Returns(Task.FromResult(recordDocuSignAction as RouteNodeDO));

                _actionMock.Setup(
                    a => a.CreateAndConfigure(It.IsAny<IUnitOfWork>(), It.IsAny<string>(), It.IsAny<int>(),
                        "StoreMTData", It.IsAny<string>(), It.IsAny<Guid>(), false)).Callback(() =>
                        {
                            using (var uow1 = ObjectFactory.GetInstance<IUnitOfWork>())
                            {
                                uow1.ActionRepository.Add(storeMtDataAction);

                                var subRoute = uow1.SubrouteRepository.GetQuery().Single();
                                subRoute.ChildNodes.Add(recordDocuSignAction);

                                uow1.SaveChanges();
                            }
                        }).Returns(Task.FromResult(storeMtDataAction as RouteNodeDO));

                ObjectFactory.Container.Inject(typeof (IAction), _actionMock.Object);
            }
        }
    }
}