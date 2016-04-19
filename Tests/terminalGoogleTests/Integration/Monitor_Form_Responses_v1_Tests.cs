﻿using System;
using NUnit.Framework;
using HealthMonitor.Utility;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.Control;
using Data.Crates;
using System.Linq;
using System.Threading.Tasks;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Newtonsoft.Json.Linq;
using terminalGoogleTests.Unit;

namespace terminalGoogleTests.Integration
{
    [Explicit]
    [Category("terminalGoogleTests.Integration")]
    public class Monitor_Form_Responses_v1_Tests : BaseTerminalIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalGoogle"; }
        }

        /// <summary>
        /// Validate correct crate-storage structure in initial configuration response.
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Initial_Configuration_Check_Crate_Structure()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();

            var requestActivityDTO = HealthMonitor_FixtureData.Monitor_Form_Responses_v1_InitialConfiguration_Fr8DataDTO();

            //Act
            var responseActivityDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActivityDTO
                );

            //Assert
            Assert.NotNull(responseActivityDTO);
            Assert.NotNull(responseActivityDTO.CrateStorage);
            Assert.NotNull(responseActivityDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActivityDTO.CrateStorage);
            Assert.AreEqual(3, crateStorage.Count);
            Assert.IsNotNull(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().SingleOrDefault());
            Assert.IsNotNull(crateStorage.CrateContentsOfType<FieldDescriptionsCM>().SingleOrDefault());
            Assert.IsNotNull(crateStorage.CrateContentsOfType<EventSubscriptionCM>().SingleOrDefault());
        }

        /// <summary>
        /// Validate correct crate-storage CM structure.
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Initial_Configuration_Check_CM_Structure()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();

            var requestActivityDTO = HealthMonitor_FixtureData.Monitor_Form_Responses_v1_InitialConfiguration_Fr8DataDTO();

            //Act
            var responseActivityDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActivityDTO
                );

            //Assert
            var crateStorage = Crate.FromDto(responseActivityDTO.CrateStorage);

            var standardConfigurationControlsCM = crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().SingleOrDefault();
            var FieldDescriptionsCM = crateStorage.CrateContentsOfType<FieldDescriptionsCM>().SingleOrDefault();
            var eventSubscriptionCM = crateStorage.CrateContentsOfType<EventSubscriptionCM>().SingleOrDefault();

            var dropdown = standardConfigurationControlsCM.Controls.Where(s => s.GetType() == typeof(DropDownList)).FirstOrDefault();

            Assert.IsNotNull(dropdown);
            Assert.AreEqual("Selected_Google_Form", dropdown.Name);
            Assert.AreEqual("Available Forms", dropdown.Source.Label);
            Assert.AreEqual(CrateManifestTypes.StandardDesignTimeFields, dropdown.Source.ManifestType);

            Assert.IsNotNull(FieldDescriptionsCM);
            Assert.AreEqual(1, crateStorage.Where(s => s.Label == "Available Forms").Count());

            Assert.IsNotNull(eventSubscriptionCM);
            Assert.AreEqual(1, crateStorage.Where(s => s.Label == "Standard Event Subscriptions").Count());
        }

        /// <summary>
        /// Validate dropdownlist source contains google forms(pre-installed in users google drive)
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Initial_Configuration_Check_Source_Fields()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();

            var requestActivityDTO = HealthMonitor_FixtureData.Monitor_Form_Responses_v1_InitialConfiguration_Fr8DataDTO();

            //Act
            var responseActivityDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActivityDTO
                );

            //Assert
            var crateStorage = Crate.FromDto(responseActivityDTO.CrateStorage);
            var FieldDescriptionsCM = crateStorage.CratesOfType<FieldDescriptionsCM>().Where(x => x.Label == "Available Forms").ToArray();

            Assert.IsNotNull(FieldDescriptionsCM);
            Assert.Greater(FieldDescriptionsCM.Count(), 0);
            Assert.Greater(FieldDescriptionsCM.First().Content.Fields.Count(), 0);
        }

        /// <summary>
        /// Wait for HTTP-500 exception when Auth-Token is not passed to initial configuration.
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        [ExpectedException(
            ExpectedException = typeof(RestfulServiceException),
            ExpectedMessage = @"{""status"":""terminal_error"",""message"":""One or more errors occurred.""}"
        )]
        public async Task Monitor_Form_Responses_Initial_Configuration_NoAuth()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActivityDTO = HealthMonitor_FixtureData.Monitor_Form_Responses_v1_InitialConfiguration_Fr8DataDTO();
            requestActivityDTO.ActivityDTO.AuthToken = null;

            await HttpPostAsync<Fr8DataDTO, JToken>(
                configureUrl,
                requestActivityDTO
            );
        }
        /// <summary>
        /// This test covers the test that the Drop Down List Box gets updated on followup configuration
        /// if it was empty after initial configuration. It needs to handle the case when Form was uploaded
        /// after the initial configuration.
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Folloup_Configuration_Updates_DDLB()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();
            var emptyActivityDTO = HealthMonitor_FixtureData.Monitor_Form_Responses_v1_InitialConfiguration_Fr8DataDTO();
            //initial configuration call
            var initialConfigurationActivityDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                configureUrl,
                emptyActivityDTO
            );
            var ddlb = GetDropDownListControl(initialConfigurationActivityDTO);
            Assert.AreEqual(0, ddlb.ListItems.Count());
            //var responseActivityDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
            //    configureUrl,
            //    initialActivityDTO
            //);
            ////Assert
            //Assert.IsNotNull(responseActivityDTO);


        }

        /// <summary>
        /// Validate google app script is uploaded in users google drive
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Activate_Check_Script_Exist()
        {
            //Arrange
            var configureUrl = GetTerminalActivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestActivityDTO = fixture.Monitor_Form_Responses_v1_ActivateDeactivate_Fr8DataDTO();

            //Act
            var responseActivityDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActivityDTO
                );

            //Assert
            Assert.IsNotNull(responseActivityDTO);

            var crateStorage = Crate.FromDto(responseActivityDTO.CrateStorage);
            var formID = crateStorage.CrateContentsOfType<StandardPayloadDataCM>().SingleOrDefault();

            Assert.Greater(formID.PayloadObjects.SelectMany(s => s.PayloadObject).Count(), 0);
        }

        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Activate_Returns_ActivityDTO()
        {
            //Arrange
            var configureUrl = GetTerminalActivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestActivityDTO = fixture.Monitor_Form_Responses_v1_ActivateDeactivate_Fr8DataDTO();

            //Act
            var responseActivityDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActivityDTO
                );

            //Assert
            Assert.IsNotNull(responseActivityDTO);
            Assert.IsNotNull(Crate.FromDto(responseActivityDTO.CrateStorage));
        }

        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Deactivate_Returns_ActivityDTO()
        {
            //Arrange
            var configureUrl = GetTerminalDeactivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestActivityDTO = fixture.Monitor_Form_Responses_v1_ActivateDeactivate_Fr8DataDTO();

            //Act
            var responseActivityDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActivityDTO
                );

            //Assert
            Assert.IsNotNull(responseActivityDTO);
            Assert.IsNotNull(Crate.FromDto(responseActivityDTO.CrateStorage));
        }

        /// <summary>
        /// Should throw exception if cannot extract any data from google form
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        [ExpectedException(
            ExpectedException = typeof(RestfulServiceException),
            ExpectedMessage = @"{""status"":""terminal_error"",""message"":""EventReportCrate is empty.""}"
            )]
        public async Task Monitor_Form_Responses_Run_WithInvalidPapertrailUrl_ShouldThrowException()
        {
            //Arrange
            var runUrl = GetTerminalRunUrl();

            //prepare the activity DTO with valid target URL
            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var activityDTO = fixture.Monitor_Form_Responses_v1_Run_EmptyPayload();
            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO };
            //Act
            await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);
        }

        /// <summary>
        /// Should return more than one payload fielddto for the response
        /// </summary>
        [Test, Category("Integration.terminalGoogle")]
        public async Task Monitor_Form_Responses_Run_Returns_Payload()
        {
            //Arrange
            var runUrl = GetTerminalRunUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var activityDTO = fixture.Monitor_Form_Responses_v1_Run_ActivityDTO();

            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO };

            AddPayloadCrate(
               dataDTO,
               new EventReportCM()
               {
                   EventPayload = new CrateStorage()
                   {
                        Data.Crates.Crate.FromContent(
                            "Response",
                            new StandardPayloadDataCM(
                                new FieldDTO("response", "key1=value1&key2=value2")
                            )
                        )
                   }
               }
            );
            //Act
            var responsePayloadDTO =
                await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);

            //Assert
            var crateStorage = Crate.FromDto(responsePayloadDTO.CrateStorage);

            var FieldDescriptionsCM = crateStorage.CrateContentsOfType<StandardPayloadDataCM>().SingleOrDefault();

            Assert.IsNotNull(FieldDescriptionsCM);
            var fields = FieldDescriptionsCM.PayloadObjects.SelectMany(s => s.PayloadObject);
            Assert.Greater(fields.Count(), 0);
        }

        private DropDownList GetDropDownListControl(ActivityDTO activityDTO)
        {
            var crateStorage = Crate.FromDto(activityDTO.CrateStorage);
            var controls = crateStorage.CratesOfType<StandardConfigurationControlsCM>();
            var ddlb = (DropDownList)controls.First(c => c.Label == "Selected_Google_Form").Content.FindByName("Selected_Google_Form");
            return ddlb;
        }
    }
}
