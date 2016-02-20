﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Data.Control;
using Data.Crates;
using NUnit.Framework;
using StructureMap;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using HealthMonitor.Utility;
using Hub.Managers;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using TerminalBase.Infrastructure;
using terminalFr8Core.Actions;
using Moq;
using Hub.Managers.APIManagers.Transmitters.Restful;
using System.Threading.Tasks;

namespace terminalFr8CoreTests.Unit
{
    [TestFixture]
    [Category("Select_Fr8_Object_v1")]
    class SelectFr8Object_v1Tests : BaseTest
    {
        IDisposable _coreServer;
        Select_Fr8_Object_v1 select_Fr8_Object_v1;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            TerminalBootstrapper.ConfigureTest();

            _coreServer = Fixtures.FixtureData.CreateCoreServer_ActivitiesController();
            select_Fr8_Object_v1 = new Select_Fr8_Object_v1();
            Mock<IRestfulServiceClient> restClientMock = new Mock<IRestfulServiceClient>(MockBehavior.Default);
            restClientMock.Setup(restClient => restClient.GetAsync<CrateDTO>(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(Task.FromResult(FixtureData.TestEmptyCrateDTO()));
            ObjectFactory.Container.Inject(typeof(IRestfulServiceClient), restClientMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (_coreServer != null)
            {
                _coreServer.Dispose();
                _coreServer = null;
            }
        }

        [Test]
        public async Task Evaluate_IsValidJSONResponse_For_InitialRequest()
        {
            ActivityDTO curActionDTO = FixtureData.TestActionDTOSelectFr8ObjectInitial();
            ActivityDO curActivityDO = Mapper.Map<ActivityDO>(curActionDTO);
            AuthorizationTokenDO curAuthTokenDO = Mapper.Map<AuthorizationTokenDO>(curActionDTO.AuthToken);
            var activity = await select_Fr8_Object_v1.Configure(curActivityDO,curAuthTokenDO);
         
            Assert.NotNull(activity);
            Assert.AreEqual(2, ObjectFactory.GetInstance<ICrateManager>().GetStorage(curActivityDO.CrateStorage).Count);
        }

        [Test]
        public async Task Evaluate_IsValidJSONResponse_For_FollowupRequest_RouteSelected()
        {
            ActivityDTO curActionDTO = FixtureData.TestActionDTOSelectFr8ObjectFollowup("19");
            ActivityDO curActivityDO = Mapper.Map<ActivityDO>(curActionDTO);
            AuthorizationTokenDO curAuthTokenDO = Mapper.Map<AuthorizationTokenDO>(curActionDTO.AuthToken);
            var activity = await select_Fr8_Object_v1.Configure(curActivityDO, curAuthTokenDO);

            Assert.NotNull(activity);
           // Assert.AreEqual(2, actionDTO.CrateStorage.CrateDTO.Count);
        }

        [Test]
        public async Task Evaluate_IsValidJSONResponse_For_FollowupRequest_ContainerSelected()
        {
            ActivityDTO curActionDTO = FixtureData.TestActionDTOSelectFr8ObjectFollowup("21");
            ActivityDO curActivityDO = Mapper.Map<ActivityDO>(curActionDTO);
            AuthorizationTokenDO curAuthTokenDO = Mapper.Map<AuthorizationTokenDO>(curActionDTO.AuthToken);
            var activity = await select_Fr8_Object_v1.Configure(curActivityDO, curAuthTokenDO);


            Assert.NotNull(activity);
            //Assert.AreEqual(2, actionDTO.CrateStorage.CrateDTO.Count);
        }
    }
}