﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Managers;
using Newtonsoft.Json;
using TerminalBase;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Utilities;

namespace terminalFr8Core.Actions
{
    public class Loop_v1 : BaseTerminalAction
    {

        public async Task<PayloadDTO> Run(ActionDO curActionDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var curPayloadDTO = await GetProcessPayload(curActionDO, containerId);
            //we used current action id to prevent mixing nested loops
            var loopIdentifierLabel = curActionDO.Id.ToString();
            var currentIndex = 0;
            using (var updater = Crate.UpdateStorage(curPayloadDTO))
            {
                var operationsData = updater.CrateStorage.CrateContentsOfType<OperationalStatusCM>(c => c.Label == loopIdentifierLabel).FirstOrDefault();
                if (operationsData == null)
                {
                    //check number of existing loops
                    var currentLoopLevel = updater.CrateStorage.CrateContentsOfType<OperationalStatusCM>().Count();
                    //this must be the first time our loop runs
                    operationsData = new OperationalStatusCM { LoopIndex = 0, Break = false, LoopLevel = currentLoopLevel };
                    var operationsCrate = Crate.CreateOperationalStatusCrate(loopIdentifierLabel, operationsData);
                    updater.CrateStorage.Add(operationsCrate);
                }
                else
                {
                    operationsData.IncreaseLoopIndex();
                    currentIndex = operationsData.LoopIndex;
                }
            }

            var manifestType = GetSelectedCrateManifestTypeToProcess(curActionDO);
            var label = GetSelectedLabelToProcess(curActionDO);

            var storage = Crate.GetStorage(curPayloadDTO);
            var crateToProcess = storage.FirstOrDefault(c =>/* c.ManifestType.Type == manifestType &&*/ c.Label == label);

            if (crateToProcess == null)
            {
                throw new TerminalCodedException(TerminalErrorCode.PAYLOAD_DATA_MISSING, "Unable to find any crate with Manifest Type: \"" + manifestType + "\" and Label: \""+label+"\"");
            }

            var data = crateToProcess.Get();
            
            //we don't know type of object we are trying to loop
            //but we are sure that it is a list
            var processList = crateToProcess.Get<List<Object>>();

            //check if we need to end this loop
            if (currentIndex > processList.Count() - 1)
            {
                using (var updater = Crate.UpdateStorage(curPayloadDTO))
                {
                    var operationsData = updater.CrateStorage.CrateContentsOfType<OperationalStatusCM>(c => c.Label == loopIdentifierLabel).Single();
                    operationsData.BreakLoop();
                }
            }

            return curPayloadDTO;
        }

        private string GetSelectedCrateManifestTypeToProcess(ActionDO curActionDO)
        {
            var controlsMS = Crate.GetStorage(curActionDO).CrateContentsOfType<StandardConfigurationControlsCM>().First();
            var manifestTypeDropdown = controlsMS.Controls.Single(x => x.Type == ControlTypes.DropDownList && x.Name == "Available_Manifests");
            if (manifestTypeDropdown.Value == null)
            {
                throw new TerminalCodedException(TerminalErrorCode.PAYLOAD_DATA_MISSING, "Loop action can't process data without a selected Manifest Type to process");
            }
            return manifestTypeDropdown.Value;
        }

        private string GetSelectedLabelToProcess(ActionDO curActionDO)
        {
            var controlsMS = Crate.GetStorage(curActionDO).CrateContentsOfType<StandardConfigurationControlsCM>().First();
            var labelDropdown = controlsMS.Controls.Single(x => x.Type == ControlTypes.DropDownList && x.Name == "Available_Labels");
            if (labelDropdown.Value == null)
            {
                throw new TerminalCodedException(TerminalErrorCode.PAYLOAD_DATA_MISSING, "Loop action can't process data without a selected Label to process");
            }
            return labelDropdown.Value;
        }

        public override async Task<ActionDO> Configure(ActionDO curActionDataPackageDO, AuthorizationTokenDO authTokenDO)
        {
            return await ProcessConfigurationRequest(curActionDataPackageDO, ConfigurationEvaluator, authTokenDO);
        }

        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            //build a controls crate to render the pane
            var configurationControlsCrate = CreateControlsCrate();

            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                updater.CrateStorage = AssembleCrateStorage(configurationControlsCrate);
                updater.CrateStorage.Add(Data.Crates.Crate.FromContent("CustomProcessConfiguration", new CustomProcessingConfigurationCM(true)));
                updater.CrateStorage.Add(await GetUpstreamManifestTypes(curActionDO));
            }

            return curActionDO;
        }

        private async Task<List<FieldDTO>> GetLabelsByManifestType(ActionDO curActionDO, string manifestType)
        {
            var upstreamCrates = await GetCratesByDirection(curActionDO, CrateDirection.Upstream);
            return upstreamCrates
                    .Where(c => c.ManifestType.Type == manifestType)
                    .GroupBy(c => c.Label)
                    .Select(c => new FieldDTO(c.Key, c.Key)).ToList();
        }

        protected override async Task<ActionDO> FollowupConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            var controlsMS = Crate.GetStorage(curActionDO).CrateContentsOfType<StandardConfigurationControlsCM>().Single();
            var manifestTypeDropdown = controlsMS.Controls.Single(x => x.Type == ControlTypes.DropDownList && x.Name == "Available_Manifests");

            if (manifestTypeDropdown.Value != null)
            {
                var labelList = await GetLabelsByManifestType(curActionDO, manifestTypeDropdown.Value);

                using (var updater = Crate.UpdateStorage(curActionDO))
                {
                    updater.CrateStorage.RemoveByLabel("Available Labels");
                    updater.CrateStorage.Add(Data.Crates.Crate.FromContent("Available Labels", new StandardDesignTimeFieldsCM() { Fields = labelList }));
                }
            }

            return curActionDO;
        }

        private async Task<Crate> GetUpstreamManifestTypes(ActionDO curActionDO)
        {
            var upstreamCrates = await GetCratesByDirection(curActionDO, CrateDirection.Upstream);
            var manifestTypeOptions = upstreamCrates.GroupBy(c => c.ManifestType).Select(c => new FieldDTO(c.Key.Type, c.Key.Type));
            var queryFieldsCrate = Crate.CreateDesignTimeFieldsCrate("Available Manifests", manifestTypeOptions.ToArray());
            return queryFieldsCrate;
        }

        private Crate CreateControlsCrate()
        {
            var infoText = new TextBlock
            {
                Value = "This Loop will process the data inside of"
            };
            var availableManifests = new DropDownList
            {
                Label = "Crate Manifest",
                Name = "Available_Manifests",
                Value = null,
                Events = new List<ControlEvent>{ new ControlEvent("onChange", "requestConfig") },
                Source = new FieldSourceDTO
                {
                    Label = "Available Manifests",
                    ManifestType = MT.StandardDesignTimeFields.GetEnumDisplayName()
                }
            };

            var availableLabels = new DropDownList
            {
                Label = "Crate Label",
                Name = "Available_Labels",
                Value = null,
                Events = new List<ControlEvent> { new ControlEvent("onChange", "requestConfig") },
                Source = new FieldSourceDTO
                {
                    Label = "Available Labels",
                    ManifestType = MT.StandardDesignTimeFields.GetEnumDisplayName()
                }
            };

            return PackControlsCrate(infoText, availableManifests, availableLabels);
        }

        private ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            if (Crate.IsStorageEmpty(curActionDO))
            {
                return ConfigurationRequestType.Initial;
            }

            var controlsMS = Crate.GetStorage(curActionDO).CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            if (controlsMS == null)
            {
                return ConfigurationRequestType.Initial;
            }

            var manifestTypeDropdown = controlsMS.Controls.FirstOrDefault(x => x.Type == ControlTypes.DropDownList && x.Name == "Available_Manifests");

            if (manifestTypeDropdown == null)
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }
    }
}