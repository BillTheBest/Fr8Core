﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Hub.Managers;
using StructureMap;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Utilities.Configuration.Azure;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using terminalAzure.Controllers;
using terminalAzure.Tests.Fixtures;

namespace terminalAzure.Tests.Controllers
{
    [TestFixture]
    public class ActionControllerTest : BaseTest
    {
        BaseTerminalController _baseTerminalController;
        private ICrateManager _crateManager;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            TerminalBootstrapper.ConfigureTest();

            CloudConfigurationManager.RegisterApplicationSettings(new AppSettingsFixture());
            
            _crateManager = ObjectFactory.GetInstance<ICrateManager>();
            _baseTerminalController = new BaseTerminalController();
        }

        [Test]
        public async void HandleDockyardRequest_TerminalTypeIsAzureSqlServer_ResponseInitialConfiguration()
        {
            string curTerminal  = "terminalAzure";
            string curActionPath = "Configure";

            ActionDTO curActionDTO = FixtureData.TestActionDTO1();

            ActionDTO actionDTO = await (Task<ActionDTO>)_baseTerminalController.HandleFr8Request(curTerminal, curActionPath, curActionDTO);

            Assert.AreEqual("Standard UI Controls", _crateManager.FromDto(actionDTO.CrateStorage).First().ManifestType.Type);
        }
    }
}