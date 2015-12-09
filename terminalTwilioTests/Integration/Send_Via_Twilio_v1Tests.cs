﻿using System.Linq;
using HealthMonitor.Utility;
using NUnit.Framework;
using terminalTwilioTests.Fixture;
using Data.Control;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers;

namespace terminalTwilioTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    class Send_Via_Twilio_v1Tests : BaseHealthMonitorTest
    {
        public override string TerminalName
        {
            get { return "terminalTwilio"; }
        }
        /// <summary>
        /// Validate correct crate-storage structure in initial configuration response.
        /// </summary>
        [Test, Category("Integration.terminalTwilio")]
        public async void Send_Via_Twilio_Initial_Configuration_Check_Crate_Structure()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();
            var requestActionDTO = HealthMonitor_FixtureData.Send_Via_Twilio_v1_InitialConfiguration_ActionDTO();
            //Act
            var responseActionDTO =
                await HttpPostAsync<ActionDTO, ActionDTO>(
                    configureUrl,
                    requestActionDTO
                );
            //Assert
            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);
            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            var controls = crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single();
            Assert.NotNull(controls.Controls[0] is TextSource);
            Assert.NotNull(controls.Controls[1] is TextBox);
            Assert.AreEqual("SMS Body", controls.Controls[1].Label);
            Assert.AreEqual("SMS_Body", controls.Controls[1].Name);
            Assert.AreEqual("Upstream Terminal-Provided Fields", controls.Controls[0].Source.Label);
            Assert.AreEqual(false, controls.Controls[0].Selected);
        }
        /// <summary>
        /// Expect null when ActionDTO with no StandardConfigurationControlsCM Crate.
        /// </summary>
        [Test, Category("Integration.terminalTwilio")]
        public async void Send_Via_Twilio_Run_With_No_SMS_Number_Provided()
        {
            //Arrange
            var runUrl = GetTerminalRunUrl();
            var curActionDTO = HealthMonitor_FixtureData.Send_Via_Twilio_v1_InitialConfiguration_ActionDTO();
            //Act
            var payloadDTO = await HttpPostAsync<ActionDTO, ActionDTO>(
                runUrl,
                curActionDTO
                );
            Assert.IsNull(payloadDTO);
        }
        /// <summary>
        /// Test Twilio Service. Preconfigure Crates with testing number.
        /// Expect that the status of the message is not fail or undelivered.
        /// </summary>
        [Test, Category("Integration.terminalTwilio")]
        public async void Send_Via_Twilio_Run_Send_SMS_With_Correct_Number()
        {
            //Arrange
            var configureUrl = GetTerminalConfigureUrl();
            var runUrl = GetTerminalRunUrl();
            var curActionDTO = HealthMonitor_FixtureData.Send_Via_Twilio_v1_InitialConfiguration_ActionDTO();
            //Act
            var responceActionDTO = await HttpPostAsync<ActionDTO, ActionDTO>(configureUrl, curActionDTO);
            var crateManager = new CrateManager();
            using (var updater = crateManager.UpdateStorage(responceActionDTO))
            {
                var curTextSource =
                    (TextSource)
                        updater.CrateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single().Controls[0];
                curTextSource.ValueSource = "specific";
                updater.CrateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single().Controls[0].Value =
                    "+15005550006";
                updater.CrateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single().Controls[1].Value =
                    "That is the body of the message";
            }
            var payloadDTO = await HttpPostAsync<ActionDTO, ActionDTO>(
                runUrl,
                responceActionDTO
                );
            //Assert
            //After Configure Test
            Assert.NotNull(responceActionDTO);
            Assert.NotNull(responceActionDTO.CrateStorage);
            Assert.NotNull(responceActionDTO.CrateStorage.Crates);
            var crateStorage = Crate.FromDto(responceActionDTO.CrateStorage);
            var controls = crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single();
            Assert.NotNull(controls.Controls[0] is TextSource);
            Assert.NotNull(controls.Controls[1] is TextBox);
            Assert.AreEqual("SMS Body", controls.Controls[1].Label);
            Assert.AreEqual("SMS_Body", controls.Controls[1].Name);
            Assert.AreEqual("Upstream Terminal-Provided Fields", controls.Controls[0].Source.Label);
            Assert.AreEqual(false, controls.Controls[0].Selected);
            //After Run Test
            var payload = Crate.FromDto(payloadDTO.CrateStorage).CrateContentsOfType<StandardPayloadDataCM>().Single();
            Assert.AreEqual("Status", payload.PayloadObjects[0].PayloadObject[0].Key);
            Assert.AreNotEqual("failed", payload.PayloadObjects[0].PayloadObject[0].Value);
            Assert.AreNotEqual("undelivered", payload.PayloadObjects[0].PayloadObject[0].Value);
        }
    }
}
