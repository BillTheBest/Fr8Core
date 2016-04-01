﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Infrastructure;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using HealthMonitor.Utility;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Hub.StructureMap;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StructureMap;
using terminalExcel.Actions;
using terminalExcelTests.Fixtures;
using TerminalBase.BaseClasses;
using Utilities.Configuration;
using Utilities.Configuration.Azure;

namespace terminalExcelTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    [Category("terminalExcel.Integration")]
    public class Load_Excel_File_v1_Tests : BaseTerminalIntegrationTest
    {
        public ICrateManager _crateManager;

        [SetUp]
        public void SetUp()
        {
            _crateManager = ObjectFactory.GetInstance<ICrateManager>();
        }

        public override string TerminalName => "terminalExcel";

        private async Task<ActivityDTO> ConfigureFollowUp(bool setFileName = false)
        {
            var configureUrl = GetTerminalConfigureUrl();
            var dataDTO = HealthMonitor_FixtureData.Load_Table_Data_v1_InitialConfiguration_Fr8DataDTO(Guid.NewGuid());
            var responseActivityDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO);
            if (setFileName)
            {
                using (var storage = _crateManager.GetUpdatableStorage(responseActivityDTO))
                {
                    var activityUi = new Load_Excel_File_v1.ActivityUi();
                    var controlsCrate = _crateManager.GetStorage(responseActivityDTO).FirstCrate<StandardConfigurationControlsCM>();
                    activityUi.SyncWith(controlsCrate.Content);
                    activityUi.FilePicker.Value = "https://yardstore1.blob.core.windows.net/default-container-dev/EmailList.xlsx";
                    storage.ReplaceByLabel(Data.Crates.Crate.FromContent(controlsCrate.Label, new StandardConfigurationControlsCM(activityUi.Controls.ToArray()), controlsCrate.Availability));
                }
            }
            dataDTO.ActivityDTO = responseActivityDTO;
            return await HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO);
        }

        [Test]
        public async Task Load_Table_Data_v1_Initial_Configuration_Check_Crate_Structure()
        {
            // Arrange
            var configureUrl = GetTerminalConfigureUrl();
            var requestActionDTO = HealthMonitor_FixtureData.Load_Table_Data_v1_InitialConfiguration_Fr8DataDTO(Guid.NewGuid());

            // Act
            var responseActionDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, requestActionDTO);

            // Assert
            Assert.NotNull(responseActionDTO, "Response from initial configuration request is null");
            Assert.NotNull(responseActionDTO.CrateStorage, "Response from initial configuration request doesn't contain crate storage");
            Assert.NotNull(responseActionDTO.CrateStorage.Crates, "Response from initial configuration request doesn't contain crates in storage");

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(), "Activity storage doesn't contain configuration controls");
            Assert.AreEqual(1, crateStorage.CratesOfType<CrateDescriptionCM>().Count(), "Activity storage doesn't contain description of runtime available crates");
        }

        [Test]
        public async Task Load_Table_Data_v1_FollowUp_Configuration_WithFileSelected_CheckCrateStructure()
        {
            // Act
            var responseFollowUpActionDTO = await ConfigureFollowUp(true);

            // Assert
            Assert.NotNull(responseFollowUpActionDTO, "Response from followup configuration request is null");
            Assert.NotNull(responseFollowUpActionDTO.CrateStorage, "Response from followup configuration request doesn't contain crate storage");
            Assert.NotNull(responseFollowUpActionDTO.CrateStorage.Crates, "Response from followup configuration request doesn't contain crates in storage");

            var crateStorage = _crateManager.GetStorage(responseFollowUpActionDTO);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(), "Activity storage doesn't contain configuration controls");
            Assert.AreEqual(1, crateStorage.CratesOfType<CrateDescriptionCM>().Count(), "Activity storage doesn't contain description of runtime available crates");
            Assert.AreEqual(1,
                            crateStorage.CratesOfType<FieldDescriptionsCM>().Count(x => x.Availability == AvailabilityType.Always),
                            "Activity storage doesn't contain crate with column headers that is avaialbe both at design time and runtime");
        }

        [Test]
        public async Task Load_Table_Data_v1_FollowUp_Configuration_WithoutFileSet_CheckCrateStructure()
        {
            // Act
            var responseFollowUpActionDTO = await ConfigureFollowUp();

            // Assert
            Assert.NotNull(responseFollowUpActionDTO, "Response from followup configuration request is null");
            Assert.NotNull(responseFollowUpActionDTO.CrateStorage, "Response from followup configuration request doesn't contain crate storage");
            Assert.NotNull(responseFollowUpActionDTO.CrateStorage.Crates, "Response from followup configuration request doesn't contain crates in storage");

            var crateStorage = _crateManager.GetStorage(responseFollowUpActionDTO);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(), "Activity storage doesn't contain configuration controls");
            Assert.AreEqual(1, crateStorage.CratesOfType<CrateDescriptionCM>().Count(), "Activity storage doesn't contain description of runtime available crates");
            Assert.AreEqual(0,
                            crateStorage.CratesOfType<FieldDescriptionsCM>().Count(x => x.Availability == AvailabilityType.Always),
                            "Activity storage shoudn't contain crate with column headers when file is not selected");
        }

        [Test]
        public async Task Load_Table_Data_v1_Run_WhenFileIsNotSelected_ResponseContainsErrorMessage()
        {
            var activityDTO = await ConfigureFollowUp();
            var runUrl = GetTerminalRunUrl();
            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO };
            dataDTO.ActivityDTO = activityDTO;
            AddOperationalStateCrate(dataDTO, new OperationalStateCM());
            
            var responsePayloadDTO = await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);

            var operationalState = Crate.GetStorage(responsePayloadDTO).FirstCrate<OperationalStateCM>().Content;
            Assert.AreEqual(ActivityErrorCode.DESIGN_TIME_DATA_MISSING, operationalState.CurrentActivityErrorCode, "Operational state should contain error response when file is not selected");
        }

        [Test]
        public async Task Load_Table_Data_v1_Run_WhenFileIsSelected_ResponseContainsTableData()
        {
            var activityDTO = await ConfigureFollowUp(true);
            var runUrl = GetTerminalRunUrl();
            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO };
            AddOperationalStateCrate(dataDTO, new OperationalStateCM());

            var responsePayloadDTO = await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);

            Assert.AreEqual(1, _crateManager.GetStorage(responsePayloadDTO).CratesOfType<StandardTableDataCM>().Count(), "Reponse payload doesn't contain table data from file");
        }

        [Test]
        public async Task Load_Table_Data_Activate_Returns_ActivityDTO()
        {
            //Arrange
            var configureUrl = GetTerminalActivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestActionDTO = HealthMonitor_FixtureData.Load_Table_Data_v1_InitialConfiguration_Fr8DataDTO(Guid.NewGuid());
            using (var storage = _crateManager.GetUpdatableStorage(requestActionDTO.ActivityDTO))
            {
                storage.Add(Data.Crates.Crate.FromContent("Control", new StandardConfigurationControlsCM(new Load_Excel_File_v1.ActivityUi().Controls.ToArray()), AvailabilityType.Configuration));
            }

                //Act
                var responseActionDTO =
                    await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                        configureUrl,
                        requestActionDTO
                    );

            //Assert
            Assert.IsNotNull(responseActionDTO);
            Assert.IsNotNull(Crate.FromDto(responseActionDTO.CrateStorage));
        }

        [Test]
        public async Task Load_Table_Data_Deactivate_Returns_ActivityDTO()
        {
            //Arrange
            var configureUrl = GetTerminalDeactivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestActionDTO = HealthMonitor_FixtureData.Load_Table_Data_v1_InitialConfiguration_Fr8DataDTO(Guid.NewGuid());
            using (var storage = _crateManager.GetUpdatableStorage(requestActionDTO.ActivityDTO))
            {
                storage.Add(Data.Crates.Crate.FromContent("Control", new StandardConfigurationControlsCM(new Load_Excel_File_v1.ActivityUi().Controls.ToArray()), AvailabilityType.Configuration));
            }

            //Act
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );

            //Assert
            Assert.IsNotNull(responseActionDTO);
            Assert.IsNotNull(Crate.FromDto(responseActionDTO.CrateStorage));
        }
    }
}
