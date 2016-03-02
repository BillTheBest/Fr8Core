﻿using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using terminalDocuSignTests.Fixtures;

namespace terminalDocuSignTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    public class Monitor_DocuSign_v1_Tests : BaseTerminalIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalDocuSign"; }
        }

        private void AssertCrateTypes(ICrateStorage crateStorage)
        {
            Assert.AreEqual(3, crateStorage.Count);

            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());
            Assert.AreEqual(1, crateStorage.CratesOfType<FieldDescriptionsCM>().Count(x => x.Label == "DocuSign Event Fields"));
            Assert.AreEqual(1, crateStorage.CratesOfType<EventSubscriptionCM>().Count(x => x.Label == "Standard Event Subscriptions"));
        }

        private void AssertControls(StandardConfigurationControlsCM controls)
        {
            Assert.AreEqual(5, controls.Controls.Count);

            // Assert that first control is a RadioButtonGroup 
            // with Label == "TemplateRecipientPicker"
            // and event: onChange => requestConfig.
            Assert.IsTrue(controls.Controls[4] is RadioButtonGroup);
            Assert.AreEqual("TemplateRecipientPicker", controls.Controls[4].Name);
            Assert.AreEqual(1, controls.Controls[4].Events.Count);
            Assert.AreEqual("onChange", controls.Controls[4].Events[0].Name);
            Assert.AreEqual("requestConfig", controls.Controls[4].Events[0].Handler);

            // Assert that 2nd-5th controls are a CheckBoxes
            // with corresponding labels and event: onChange => requestConfig.
            var checkBoxLabels = new[] {
                "Event_Envelope_Sent",
                "Event_Envelope_Received",
                "Event_Recipient_Signed"
            };

            for (var i = 0; i < checkBoxLabels.Length; ++i)
            {
                var ci = i + 1;

                Assert.IsTrue(controls.Controls[ci] is CheckBox);
                Assert.AreEqual(checkBoxLabels[i], controls.Controls[ci].Name);
                Assert.AreEqual(1, controls.Controls[ci].Events.Count);
                Assert.AreEqual("onChange", controls.Controls[ci].Events[0].Name);
                Assert.AreEqual("requestConfig", controls.Controls[ci].Events[0].Handler);
            }

            // Assert that radio group contains two radios labeled "recipient" and "template".
            var radioButtonGroup = (RadioButtonGroup)controls.Controls[4];
            Assert.AreEqual(2, radioButtonGroup.Radios.Count);
            Assert.AreEqual("recipient", radioButtonGroup.Radios[0].Name);
            Assert.AreEqual("template", radioButtonGroup.Radios[1].Name);

            // Assert that "recipient" radio contains single TextBox control
            // labeled "RecipientValue" with event "onChange" => "requestConfig".
            Assert.AreEqual(1, radioButtonGroup.Radios[0].Controls.Count);
            Assert.IsTrue(radioButtonGroup.Radios[0].Controls[0] is TextBox);
            Assert.AreEqual("RecipientValue", radioButtonGroup.Radios[0].Controls[0].Name);
            Assert.AreEqual(1, radioButtonGroup.Radios[0].Controls[0].Events.Count);
            Assert.AreEqual("onChange", radioButtonGroup.Radios[0].Controls[0].Events[0].Name);
            Assert.AreEqual("requestConfig", radioButtonGroup.Radios[0].Controls[0].Events[0].Handler);

            // Assert that "recipient" radio contains single DropDownList control
            // labeled "UpstreamCrate", with event "onChange" => "requestConfig",
            // with source labeled "Available Templates".
            Assert.AreEqual(1, radioButtonGroup.Radios[1].Controls.Count);
            Assert.IsTrue(radioButtonGroup.Radios[1].Controls[0] is DropDownList);
            Assert.AreEqual("UpstreamCrate", radioButtonGroup.Radios[1].Controls[0].Name);
            Assert.AreEqual(null, radioButtonGroup.Radios[1].Controls[0].Source);
            Assert.AreEqual(1, radioButtonGroup.Radios[1].Controls[0].Events.Count);
            Assert.AreEqual("onChange", radioButtonGroup.Radios[1].Controls[0].Events[0].Name);
            Assert.AreEqual("requestConfig", radioButtonGroup.Radios[1].Controls[0].Events[0].Handler);
        }

        private async Task<ActivityDTO> GetActivityDTO_WithRecipientValue()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );

            responseActionDTO.AuthToken = HealthMonitor_FixtureData.DocuSign_AuthToken();

            using (var crateStorage = Crate.GetUpdatableStorage(responseActionDTO))
            {
                var controls = crateStorage
                    .CrateContentsOfType<StandardConfigurationControlsCM>()
                    .Single();

                var radioGroup = (RadioButtonGroup)controls.Controls[4];
                radioGroup.Radios[0].Selected = true;
                radioGroup.Radios[1].Selected = false;

                var recipientTextBox = (TextBox)radioGroup.Radios[0].Controls[0];
                recipientTextBox.Value = "foo@bar.com";
            }

            return responseActionDTO;
        }

        private async Task<Tuple<ActivityDTO, string>> GetActivityDTO_WithTemplateValue()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );

            responseActionDTO.AuthToken = HealthMonitor_FixtureData.DocuSign_AuthToken();

            string selectedTemplate = null;
            using (var crateStorage = Crate.GetUpdatableStorage(responseActionDTO))
            {
                var controls = crateStorage
                    .CrateContentsOfType<StandardConfigurationControlsCM>()
                    .Single();

                var radioGroup = (RadioButtonGroup)controls.Controls[4];
                radioGroup.Radios[0].Selected = false;
                radioGroup.Radios[1].Selected = true;

                var templateDdl = (DropDownList)radioGroup.Radios[1].Controls[0];

                Assert.IsTrue(templateDdl.ListItems.Count > 0);

                templateDdl.Value = templateDdl.ListItems[0].Value;
                selectedTemplate = templateDdl.ListItems[0].Key;
            }

            return new Tuple<ActivityDTO, string>(responseActionDTO, selectedTemplate);
        }

        /// <summary>
        /// Validate correct crate-storage structure in initial configuration response.
        /// </summary>
        [Test]
        public async Task Monitor_DocuSign_Initial_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            AssertCrateTypes(crateStorage);
            AssertControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        /// <summary>
        /// Wait for HTTP-500 exception when Auth-Token is not passed to initial configuration.
        /// </summary>
        [Test]
        [ExpectedException(
            ExpectedException = typeof(RestfulServiceException),
            ExpectedMessage = @"{""status"":""terminal_error"",""message"":""One or more errors occurred.""}",
            MatchType = MessageMatch.Contains
        )]
        public async Task Monitor_DocuSign_Initial_Configuration_NoAuth()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestDataDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();
            requestDataDTO.ActivityDTO.AuthToken = null;

            await HttpPostAsync<Fr8DataDTO, JToken>(
                configureUrl,
                requestDataDTO
            );
        }

        /// <summary>
        /// Validate correct crate-storage structure in follow-up configuration response.
        /// </summary>
        [Test]
        public async Task Monitor_DocuSign_FollowUp_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestDataDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestDataDTO
                );

            responseActionDTO.AuthToken = HealthMonitor_FixtureData.DocuSign_AuthToken();
            requestDataDTO.ActivityDTO = responseActionDTO;
            responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestDataDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            AssertCrateTypes(crateStorage);
            AssertControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        /// <summary>
        /// Set Selected property to True of "recipient" control
        /// and set "RecipientValue" control's value to some email. 
        /// Trigger FollowUp configuration method. 
        /// Assert that result contains crate from step design-time crate labeled "DocuSign Event Fields", 
        /// that contains single field with key = "TemplateId" and empty value.
        /// </summary>
        [Test]
        public async Task Monitor_DocuSign_FollowUp_Configuration_RecipientValue()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var activityDTO = await GetActivityDTO_WithRecipientValue();
            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO };
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );

            var crateStorage = Crate.GetStorage(responseActionDTO);
            var docuSignEventFields = crateStorage
                .CrateContentsOfType<FieldDescriptionsCM>(x => x.Label == "DocuSign Event Fields")
                .Single();

            Assert.AreEqual(12, docuSignEventFields.Fields.Count);
        }

        /// <summary>
        /// Set Selected property to True of "template" control
        /// and set "UpstreamCrate" control's value to first value 
        /// from "Available Templates" crate. 
        /// Assert that result contains design-time crate labeled "DocuSign Event Fields",
        /// that contains single field with key = "TemplateId", 
        /// the value of that field should be equal to what was set to "UpstreamCrate" drop-down-list.
        /// </summary>
        [Test]
        public async Task Monitor_DocuSign_FollowUp_Configuration_TemplateValue()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var activityDTO = await GetActivityDTO_WithTemplateValue();
            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO.Item1 };
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );

            var crateStorage = Crate.GetStorage(responseActionDTO);
            var docuSignEventFields = crateStorage
                .CrateContentsOfType<FieldDescriptionsCM>(x => x.Label == "DocuSign Event Fields")
                .Single();

            Assert.AreEqual(12, docuSignEventFields.Fields.Count());
        }

        /// <summary>
        /// Wait for HTTP-500 exception when Auth-Token is not passed to initial configuration.
        /// </summary>
        [Test]
        [ExpectedException(
            ExpectedException = typeof(RestfulServiceException),
            ExpectedMessage = @"{""status"":""terminal_error"",""message"":""One or more errors occurred.""}",
            MatchType = MessageMatch.Contains
        )]
        public async Task Monitor_DocuSign_FollowUp_Configuration_NoAuth()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestDataDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestDataDTO
                );
            requestDataDTO.ActivityDTO = responseActionDTO;
            await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                configureUrl,
                requestDataDTO
            );
        }

        /// <summary>
        /// Test run-time for action from Monitor_DocuSign_FollowUp_Configuration_RecipientValue.
        /// </summary>
        [Test]
        public async Task Monitor_DocuSign_Run_RecipientValue()
        {
            var envelopeId = Guid.NewGuid().ToString();

            var runUrl = GetTerminalRunUrl();

            var activityDTO = await GetActivityDTO_WithRecipientValue();

            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO };

            AddPayloadCrate(
                dataDTO,
                new EventReportCM()
                {
                    EventPayload = new CrateStorage()
                    {
                        Data.Crates.Crate.FromContent(
                            "EventReport",
                            new StandardPayloadDataCM(
                                new FieldDTO("RecipientEmail", "foo@bar.com"),
                                new FieldDTO("EnvelopeId", envelopeId)
                            )
                        )
                    }
                }
            );

            AddOperationalStateCrate(dataDTO, new OperationalStateCM());

            var responsePayloadDTO =
                await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);

            var crateStorage = Crate.GetStorage(responsePayloadDTO);
            Assert.AreEqual(1, crateStorage.CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "DocuSign Envelope Payload Data").Count());

            var docuSignPayload = crateStorage.CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "DocuSign Envelope Payload Data").Single();
            Assert.AreEqual(1, docuSignPayload.AllValues().Count(x => x.Key == "RecipientEmail"));
            Assert.IsTrue(docuSignPayload.AllValues().Any(x => x.Key == "RecipientEmail" && x.Value == "foo@bar.com"));
        }

        /// <summary>
        /// Test run-time for action from Monitor_DocuSign_FollowUp_Configuration_TemplateValue.
        /// </summary>
        [Test, Ignore]
        public async Task Monitor_DocuSign_Run_TemplateValue()
        {
            var envelopeId = Guid.NewGuid().ToString();

            var configureUrl = GetTerminalConfigureUrl();
            var runUrl = GetTerminalRunUrl();

            var activityDTO = await GetActivityDTO_WithTemplateValue();
            activityDTO.Item1.AuthToken = HealthMonitor_FixtureData.DocuSign_AuthToken();
            var dataDTO = new Fr8DataDTO { ActivityDTO = activityDTO.Item1 };
            var preparedActionDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO);
            dataDTO.ActivityDTO = preparedActionDTO;
            AddOperationalStateCrate(dataDTO, new OperationalStateCM());

            AddPayloadCrate(
                dataDTO,
                new EventReportCM()
                {
                    EventPayload = new CrateStorage()
                    {
                        Data.Crates.Crate.FromContent(
                            "EventReport",
                            new StandardPayloadDataCM(
                                new FieldDTO("TemplateName", activityDTO.Item2),
                                new FieldDTO("EnvelopeId", envelopeId)
                            )
                        )
                    }
                }
            );

            preparedActionDTO.AuthToken = HealthMonitor_FixtureData.DocuSign_AuthToken();

            var responsePayloadDTO =
                await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);

            var crateStorage = Crate.GetStorage(responsePayloadDTO);
            Assert.AreEqual(1, crateStorage.CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "DocuSign Envelope Payload Data").Count());

            var docuSignPayload = crateStorage.CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "DocuSign Envelope Payload Data").Single();
        }

        /// <summary>
        /// Test run-time without Auth-Token.
        /// </summary>
        [Test]
        public async Task Monitor_DocuSign_Run_NoAuth()
        {
            var runUrl = GetTerminalRunUrl();

            var requestDataDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();
            requestDataDTO.ActivityDTO.AuthToken = null;
            AddOperationalStateCrate(requestDataDTO, new OperationalStateCM());
            var payload = await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, requestDataDTO);
            CheckIfPayloadHasNeedsAuthenticationError(payload);
        }

        [Test]
        public async Task Monitor_DocuSign_Activate_Returns_ActivityDTO()
        {
            //Arrange
            var configureUrl = GetTerminalActivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestDataDTO = HealthMonitor_FixtureData.Mail_Merge_Into_DocuSign_v1_InitialConfiguration_Fr8DataDTO();
            using (var crateStorage = Crate.GetUpdatableStorage(requestDataDTO.ActivityDTO))
            {
                crateStorage.Add(Crate.CreateStandardConfigurationControlsCrate("Configuration_Controls", new ControlDefinitionDTO[] { }));
            }


            //Act
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestDataDTO
                );

            //Assert
            Assert.IsNotNull(responseActionDTO);
            Assert.IsNotNull(Crate.FromDto(responseActionDTO.CrateStorage));
        }

        [Test]
        public async Task Monitor_DocuSign_Deactivate_Returns_ActivityDTO()
        {
            //Arrange
            var configureUrl = GetTerminalDeactivateUrl();

            HealthMonitor_FixtureData fixture = new HealthMonitor_FixtureData();
            var requestDataDTO = HealthMonitor_FixtureData.Monitor_DocuSign_v1_InitialConfiguration_Fr8DataDTO();
            using (var crateStorage = Crate.GetUpdatableStorage(requestDataDTO.ActivityDTO))
            {
                crateStorage.Add(Crate.CreateStandardConfigurationControlsCrate("Configuration_Controls", new ControlDefinitionDTO[] { }));
            }
            //Act
            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestDataDTO
                );

            //Assert
            Assert.IsNotNull(responseActionDTO);
            Assert.IsNotNull(Crate.FromDto(responseActionDTO.CrateStorage));
        }
    }
}
