﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Hub.StructureMap;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StructureMap;
using UtilitiesTesting.Fixtures;

namespace terminalDocuSignTests
{
    [Explicit]
    public class Send_DocuSign_Envelope_v1_Tests : BaseHealthMonitorTest
    {

        public ICrateManager _crateManager;

        [SetUp]
        public void SetUp()
        {
            StructureMapBootStrapper.ConfigureDependencies(StructureMapBootStrapper.DependencyType.TEST);        
            _crateManager = ObjectFactory.GetInstance<ICrateManager>();
        }

        public override string TerminalName
        {
            get { return "terminalDocuSign"; }
        }

        private async Task<ActionDTO> ConfigureInitial()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Send_DocuSign_Envelope_v1_Example_ActionDTO();
            var responseActionDTO = await HttpPostAsync<ActionDTO, ActionDTO>(configureUrl, requestActionDTO);

            return responseActionDTO;
        }

        private async Task<ActionDTO> ConfigureFollowUp()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Send_DocuSign_Envelope_v1_Example_ActionDTO();

            var responseActionDTO = await HttpPostAsync<ActionDTO, ActionDTO>(configureUrl, requestActionDTO);

            var storage = _crateManager.GetStorage(responseActionDTO);

            SendDocuSignEnvelope_SelectFirstTemplate(storage);

            using (var updater = _crateManager.UpdateStorage(requestActionDTO))
            {
                updater.CrateStorage = storage;
            }

            return await HttpPostAsync<ActionDTO, ActionDTO>(configureUrl, requestActionDTO);
        }

        private void AssertCrateTypes(CrateStorage crateStorage)
        {
            Assert.AreEqual(3, crateStorage.Count);

            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(x => x.Label == "Configuration_Controls"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Available Templates"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Upstream Terminal-Provided Fields"));
            
        }

        private void AssertFollowUpCrateTypes(CrateStorage crateStorage)
        {
            Assert.AreEqual(5, crateStorage.Count);

            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(x => x.Label == "Configuration_Controls"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Available Templates"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "DocuSignTemplateUserDefinedFields"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "DocuSignTemplateStandardFields"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Upstream Terminal-Provided Fields"));
        }
        
        private void AssertControls(StandardConfigurationControlsCM controls)
        {
            Assert.AreEqual(2, controls.Controls.Count);

            // Assert that first control is a DropDownList 
            // with Label == "target_docusign_template"
            // and event: onChange => requestConfig.
            Assert.IsTrue(controls.Controls[0] is DropDownList);
            Assert.AreEqual("target_docusign_template", controls.Controls[0].Name);
            Assert.AreEqual(1, controls.Controls[0].Events.Count);
            Assert.AreEqual("onChange", controls.Controls[0].Events[0].Name);
            Assert.AreEqual("requestConfig", controls.Controls[0].Events[0].Handler);

            // Assert that second control is a TextSource 
            // with Label == "Recipient"
            // and event: null
            Assert.IsTrue(controls.Controls[1] is TextSource);
            Assert.AreEqual("Recipient", controls.Controls[1].Name);
            Assert.IsNull(controls.Controls[1].Events);   
        }

        /// <summary>
        /// Validate correct crate-storage structure in initial configuration response.
        /// </summary>
        [Test]
        public async void Send_DocuSign_Initial_Configuration_Check_Crate_Structure()
        {
            var responseActionDTO = await ConfigureInitial();

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
            ExpectedMessage = @"{""status"":""terminal_error"",""message"":""One or more errors occurred.""}"
        )]
        public async void Send_DocuSign_Initial_Configuration_NoAuth()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Send_DocuSign_Envelope_v1_Example_ActionDTO();
            requestActionDTO.AuthToken = null;

            await HttpPostAsync<ActionDTO, JToken>(
                configureUrl,
                requestActionDTO
            );
        }

        /// <summary>
        /// Validate correct crate-storage structure in follow-up configuration response.
        /// </summary>
        [Test]
        public async void Send_DocuSign_FollowUp_Configuration_Check_Crate_Structure()
        {
            var responseFollowUpActionDTO = await ConfigureFollowUp();

            Assert.NotNull(responseFollowUpActionDTO);
            Assert.NotNull(responseFollowUpActionDTO.CrateStorage);
            Assert.NotNull(responseFollowUpActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseFollowUpActionDTO.CrateStorage);
            AssertFollowUpCrateTypes(crateStorage);
            AssertControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        /// <summary>
        /// Wait for HTTP-500 exception when Auth-Token is not passed to followup configuration.
        /// </summary>
        [Test]
        [ExpectedException(
            ExpectedException = typeof(RestfulServiceException),
            ExpectedMessage = @"{""status"":""terminal_error"",""message"":""One or more errors occurred.""}"
        )]
        public async void Send_DocuSign_FollowUp_Configuration_NoAuth()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Send_DocuSign_Envelope_v1_Example_ActionDTO();

            var responseActionDTO = await HttpPostAsync<ActionDTO, ActionDTO>(configureUrl, requestActionDTO);

            var storage = _crateManager.GetStorage(responseActionDTO);

            SendDocuSignEnvelope_SelectFirstTemplate(storage);

            using (var updater = _crateManager.UpdateStorage(requestActionDTO))
            {
                updater.CrateStorage = storage;
            }

            requestActionDTO.AuthToken = null;

            await HttpPostAsync<ActionDTO, ActionDTO>(configureUrl, requestActionDTO);
        }

        /// <summary>
        /// Test run-time for action from Monitor_DocuSign_FollowUp_Configuration_TemplateValue.
        /// </summary>
        [Test]
        public async void Send_DocuSign_Run()
        {
            var runUrl = GetTerminalRunUrl();

            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = HealthMonitor_FixtureData.Send_DocuSign_Envelope_v1_Example_ActionDTO();

            var responseActionDTO = await HttpPostAsync<ActionDTO, ActionDTO>(configureUrl, requestActionDTO);

            var storage = _crateManager.GetStorage(responseActionDTO);

            SendDocuSignEnvelope_SelectFirstTemplate(storage);

            using (var updater = _crateManager.UpdateStorage(requestActionDTO))
            {
                updater.CrateStorage = storage;
            }

            
            var responsePayloadDTO = await HttpPostAsync<ActionDTO, PayloadDTO>(runUrl, requestActionDTO);

            var crateStorage = Crate.GetStorage(responsePayloadDTO);
            Assert.AreEqual(1, crateStorage.CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "DocuSign Envelope Payload Data").Count());

        }


        private void SendDocuSignEnvelope_SelectFirstTemplate(CrateStorage curCrateStorage)
        {
            // Fetch Available Template crate and parse StandardDesignTimeFieldsMS.
            var availableTemplatesCrateDTO = curCrateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Single(x => x.Label == "Available Templates");

            var fieldsMS = availableTemplatesCrateDTO.Content;

            // Fetch Configuration Controls crate and parse StandardConfigurationControlsMS

            var configurationControlsCrateDTO = curCrateStorage.CratesOfType<StandardConfigurationControlsCM>().Single(x => x.Label == "Configuration_Controls");

            var controlsMS = configurationControlsCrateDTO.Content;

            // Modify value of Selected_DocuSign_Template field and push it back to crate,
            // exact same way we do on front-end.
            var docuSignTemplateControlDTO = controlsMS.Controls.Single(x => x.Name == "target_docusign_template");
            docuSignTemplateControlDTO.Value = fieldsMS.Fields.First().Value;
        }
    }
}
