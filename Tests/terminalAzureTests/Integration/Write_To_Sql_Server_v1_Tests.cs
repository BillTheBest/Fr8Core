﻿using System.Linq;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using Hub.Managers;
using Hub.StructureMap;
using NUnit.Framework;
using StructureMap;
using terminalAzureTests.Fixtures;
using UtilitiesTesting.Fixtures;
using System.Collections.Generic;

namespace terminalAzureTests.Integration
{
    [Explicit]
    public class Write_To_Sql_Server_v1_Tests : BaseHealthMonitorTest
    {
        public override string TerminalName
        {
            get { return "terminalAzure"; }
        }

        public ICrateManager _crateManager;

        [SetUp]
        public void SetUp()
        {
            StructureMapBootStrapper.ConfigureDependencies(StructureMapBootStrapper.DependencyType.TEST);
            _crateManager = ObjectFactory.GetInstance<ICrateManager>();
        }

        private void AssertConfigureControls(StandardConfigurationControlsCM control)
        {
            Assert.AreEqual(1, control.Controls.Count);

            // Assert that first control is a TextBox 
            // with Label == "SQL Connection String"
            // with Name == "connection_string"
            Assert.IsTrue(control.Controls[0] is TextBox);
            Assert.AreEqual("SQL Connection String", control.Controls[0].Label);
            Assert.AreEqual("connection_string", control.Controls[0].Name);
        }

        private void AssertConfigureCrate(CrateStorage crateStorage)
        {
            Assert.AreEqual(1, crateStorage.Count);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());

            AssertConfigureControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        private Crate CreateConnectionStringCrate()
        {
            var control = FixtureData.TestConnectionString2();
            control.Name = "connection_string";
            control.Label = "SQL Connection String";

            return PackControlsCrate(control);
        }
        
        private Crate<StandardConfigurationControlsCM> PackControlsCrate(params ControlDefinitionDTO[] controlsList)
        {
            return Crate<StandardConfigurationControlsCM>.FromContent("Configuration_Controls", new StandardConfigurationControlsCM(controlsList));
        }

        /// <summary>
        /// Validate correct crate-storage structure in initial configuration response.
        /// </summary>
        [Test]
        public async void Write_To_Sql_Server_Initial_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Write_To_Sql_Server_v1_InitialConfiguration_ActionDTO();

            var responseActionDTO =
                await HttpPostAsync<ActionDTO, ActionDTO>(
                    configureUrl,
                    requestActionDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            AssertConfigureCrate(crateStorage);
        }

        /// <summary>
        /// Validate correct crate-storage structure in follow-up configuration response 
        /// </summary>
        [Test]
        public async void Write_To_Sql_Server_FollowUp_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Write_To_Sql_Server_v1_InitialConfiguration_ActionDTO();

            var responseActionDTO =
                await HttpPostAsync<ActionDTO, ActionDTO>(
                    configureUrl,
                    requestActionDTO
                );
            

            var storage = _crateManager.GetStorage(responseActionDTO);

            var controlDefinitionDTO =
                storage.CratesOfType<StandardConfigurationControlsCM>()
                    .Select(x => x.Content.FindByName("connection_string")).ToArray();

            controlDefinitionDTO[0].Value = FixtureData.TestConnectionString2().Value;

            using (var updater = _crateManager.UpdateStorage(responseActionDTO))
            {
                updater.CrateStorage = storage;
            }

            responseActionDTO =
                await HttpPostAsync<ActionDTO, ActionDTO>(
                    configureUrl,
                    responseActionDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);

            Assert.AreEqual(2, crateStorage.Count);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());

            AssertConfigureControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());

            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count());
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Sql Table Columns"));
        }

        /// <summary>
        /// Test run-time for action Run().
        /// </summary>
        [Test]
        public async void Write_To_Sql_Server_Run()
        {
            var runUrl = GetTerminalRunUrl();

            var actionDTO = HealthMonitor_FixtureData.Write_To_Sql_Server_v1_InitialConfiguration_ActionDTO();
            
            using (var updater = Crate.UpdateStorage(actionDTO))
            {
                updater.CrateStorage.Add(CreateConnectionStringCrate());
            }
            
            AddPayloadCrate(
               actionDTO,
               new StandardPayloadDataCM(
                    new FieldDTO("Field1", "[Customer].[Physician]"),
                    new FieldDTO("Field2", "[Customer].[CurrentMedicalCondition]")
               ),
               "MappedFields"
            );

            AddPayloadCrate(
                actionDTO,
                new StandardPayloadDataCM(
                    new FieldDTO("Field1", "test physician"),
                    new FieldDTO("Field2", "teststring")
                ),
               "DocuSign Envelope Data"
            );

            var responsePayloadDTO =
                await HttpPostAsync<ActionDTO, PayloadDTO>(runUrl, actionDTO);

            Assert.NotNull(responsePayloadDTO);
            Assert.NotNull(responsePayloadDTO.CrateStorage);
            Assert.NotNull(responsePayloadDTO.CrateStorage.Crates);
        }
    }
}
