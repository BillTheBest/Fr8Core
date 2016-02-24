﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocuSign.Integrations.Client;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;

using Hub.Managers;
using Utilities;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Infrastructure;
using terminalDocuSign.Interfaces;
using terminalDocuSign.Services;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using TerminalBase.Infrastructure.Behaviors;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace terminalDocuSign.Actions
{
    public class Send_DocuSign_Envelope_v1 : BaseDocuSignActivity
    {
        private DocuSignManager _docuSignManager = new DocuSignManager();
        public Send_DocuSign_Envelope_v1()
        {
        }

        public override async Task<ActivityDO> Configure(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            CheckAuthentication(authTokenDO);

            return await ProcessConfigurationRequest(curActivityDO, ConfigurationEvaluator, authTokenDO);
        }

        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(curActivityDO, containerId);

            if (NeedsAuthentication(authTokenDO))
            {
                return NeedsAuthenticationError(payloadCrates);
            }

            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthTokenDTO>(authTokenDO.Token);

            var curEnvelope = new Envelope();
            curEnvelope.Login = new DocuSignPackager()
                .Login(docuSignAuthDTO.Email, docuSignAuthDTO.ApiPassword);

            try
            {
                curEnvelope = AddTemplateData(curActivityDO, payloadCrates, curEnvelope);
            }
            catch (ApplicationException exception)
            {
                //in case of problem with extract payload field values raise and Error alert to the user
                return Error(payloadCrates, exception.Message, null, "Send DocuSign Envelope", "DocuSign");
            }

            curEnvelope.EmailSubject = "Test Message from Fr8";
            curEnvelope.Status = "sent";

            var result = curEnvelope.Create();

            return Success(payloadCrates);
        }

        private string ExtractTemplateId(ActivityDO curActivityDO)
        {
            var controls = CrateManager.GetStorage(curActivityDO).CrateContentsOfType<StandardConfigurationControlsCM>().First().Controls;

            var templateDropDown = controls.SingleOrDefault(x => x.Name == "target_docusign_template");

            if (templateDropDown == null)
            {
                throw new ApplicationException("Could not find target_docusign_template DropDownList control.");
            }

            var result = templateDropDown.Value;
            return result;
        }

        private Envelope AddTemplateData(ActivityDO curActivityDO, PayloadDTO payloadCrates, Envelope curEnvelope)
        {
            var curTemplateId = ExtractTemplateId(curActivityDO);
            var payloadCrateStorage = CrateManager.GetStorage(payloadCrates);
            var configurationControls = GetConfigurationControls(curActivityDO);
            var recipientField = (TextSource)GetControl(configurationControls, "Recipient", ControlTypes.TextSource);
            var recipientNameField = (TextSource)GetControl(configurationControls, "RecipientName", ControlTypes.TextSource);

            var curRecipientAddress = recipientField.GetValue(payloadCrateStorage, true);
            var curRecipientName = recipientNameField.GetValue(payloadCrateStorage, true);

            curEnvelope.TemplateId = curTemplateId;

            var templateRoles = new TemplateRole[]
            {
                    new TemplateRole()
                    {
                        email = curRecipientAddress,
                        name = curRecipientName,
                        roleName = "Signer"   // need to fetch this
                    },
            };  

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                var mappingBehavior = new TextSourceMappingBehavior(
                    crateStorage,   
                    "Mapping"
                );

                var values = mappingBehavior.GetValues(payloadCrateStorage);

                var valuesToAdd = new List<RoleTextTab>();
                var nonEmptyValues = values.Where(x => !string.IsNullOrEmpty(x.Value));
                foreach (var pair in nonEmptyValues)
                {

                    valuesToAdd.Add(new RoleTextTab()
                    {
                        tabLabel = pair.Key,
                        value = pair.Value,
                    });
                }
                curEnvelope.TemplateRoles = templateRoles;
                curEnvelope.TemplateRoles[0].tabs = new RoleTabs();
                curEnvelope.TemplateRoles[0].tabs.textTabs = valuesToAdd.ToArray();

                //set radio button tabs

                var radiopGroupMappingBehavior = new RadioButtonGroupMappingBehavior(crateStorage);
                var radioButtonGroups = radiopGroupMappingBehavior.GetValues(payloadCrateStorage).ToList();
                
                //curEnvelope.TemplateRoles[0].tabs.
                var radioGroupTabsToAdd = new List<TemplateRadioGroupTab>();
                foreach (RadioButtonGroup item in radioButtonGroups)
                {
                    radioGroupTabsToAdd.Add(new TemplateRadioGroupTab()
                    {
                        groupName = item.GroupName,
                        radios = item.Radios.Select(x=> new radio()
                        {
                            selected = x.Selected,
                            value = x.Value
                        }).ToArray()
                    });
                }

                curEnvelope.TemplateRoles[0].tabs.radioGroupTabs = radioGroupTabsToAdd.ToArray();
            }

            curEnvelope.TemplateRoles = templateRoles;
            return curEnvelope;
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            // Do we have any crate? If no, it means that it's Initial configuration
            if (CrateManager.IsStorageEmpty(curActivityDO))
            {
                return ConfigurationRequestType.Initial;
            }

            // Try to find Configuration_Controls
            var stdCfgControlMS = CrateManager.GetStorage(curActivityDO).CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();
            if (stdCfgControlMS == null)
            {
                return ConfigurationRequestType.Initial;
            }

            // Try to get DropdownListField
            var dropdownControlDTO = stdCfgControlMS.FindByName("target_docusign_template");
            if (dropdownControlDTO == null)
            {
                return ConfigurationRequestType.Initial;
            }

            var docusignTemplateId = dropdownControlDTO.Value;
            if (string.IsNullOrEmpty(docusignTemplateId))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected override async Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthTokenDTO>(authTokenDO.Token);

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                // Only do it if no existing MT.FieldDescription crate is present to avoid loss of existing settings
                // Two crates are created
                // One to hold the ui controls
                if (crateStorage.All(c => c.ManifestType.Id != (int)MT.FieldDescription))
                {
                    var crateControlsDTO = CreateDocusignTemplateConfigurationControls(curActivityDO);
                    // and one to hold the available templates, which need to be requested from docusign
                    var crateDesignTimeFieldsDTO = _docuSignManager.PackCrate_DocuSignTemplateNames(docuSignAuthDTO);

                    crateStorage.Replace(new CrateStorage(crateControlsDTO, crateDesignTimeFieldsDTO));
                }

                await UpdateUpstreamCrate(curActivityDO, crateStorage);
            }



            return curActivityDO;
        }

        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthTokenDTO>(authTokenDO.Token);

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                if (crateStorage.Count == 0)
                {
                    return curActivityDO;
                }

                await UpdateUpstreamCrate(curActivityDO, crateStorage);

                // Try to find Configuration_Controls.
                var stdCfgControlMS = crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();
                if (stdCfgControlMS == null)
                {
                    return curActivityDO;
                }

                // Try to find DocuSignTemplate drop-down.
                var dropdownControlDTO = stdCfgControlMS.FindByName("target_docusign_template");
                if (dropdownControlDTO == null)
                {
                    return curActivityDO;
                }

                // Get DocuSign Template Id
                var docusignTemplateId = dropdownControlDTO.Value;

                // Get Template
                var docuSignEnvelope = new DocuSignEnvelope(docuSignAuthDTO.Email, docuSignAuthDTO.ApiPassword);
                var envelopeDataDTO = docuSignEnvelope.GetEnvelopeDataByTemplate(docusignTemplateId).ToList();

                // when we're in design mode, there are no values
                // we just want the names of the fields
                var userDefinedFields = new List<FieldDTO>();
                var userDefinedGroupFields = new List<FieldDTO>();

                var userDefinedGroupItems = new List<GroupWrapperEnvelopeDataDTO>();
                
                foreach (var x in envelopeDataDTO)
                {
                    //special case for radio buttons
                    if (x is GroupWrapperEnvelopeDataDTO)
                    {
                        userDefinedGroupFields.Add(new FieldDTO() { Key = x.Name, Value = x.Name, Availability = AvailabilityType.RunTime });
                        //add check here for has key
                        userDefinedGroupItems.Add((GroupWrapperEnvelopeDataDTO) x);
                    }
                    else
                    {
                        userDefinedFields.Add(new FieldDTO() { Key = x.Name, Value = x.Name, Availability = AvailabilityType.RunTime });
                    }
                }

                // we're in design mode, there are no values 
                var standartFields = new List<FieldDTO>()
                {
                    new FieldDTO() {Key = "recipient", Value = "recipient", Availability = AvailabilityType.RunTime }
                };

                var crateUserDefinedDTO = CrateManager.CreateDesignTimeFieldsCrate(
                    "DocuSignTemplateUserDefinedFields",
                    userDefinedFields.Concat(userDefinedGroupFields).ToArray()
                );

                var crateStandardDTO = CrateManager.CreateDesignTimeFieldsCrate(
                    "DocuSignTemplateStandardFields",
                    standartFields.ToArray()
                );

                crateStorage.RemoveByLabel("DocuSignTemplateUserDefinedFields");
                crateStorage.RemoveByLabel("DocuSignTemplateStandardFields");
                crateStorage.Add(crateUserDefinedDTO);
                crateStorage.Add(crateStandardDTO);

                var allFields = new List<string>();
                allFields.AddRange(userDefinedFields.Select(x => x.Key));
                allFields.AddRange(standartFields.Select(x => x.Key));

                var mappingBehavior = new TextSourceMappingBehavior(
                    crateStorage,
                    "Mapping"
                );
                mappingBehavior.Clear();
                mappingBehavior.Append(allFields, "Upstream Terminal-Provided Fields");

                var radioButtonGroupBehavior = new RadioButtonGroupMappingBehavior(
                    crateStorage);

                radioButtonGroupBehavior.Clear();
                radioButtonGroupBehavior.Append(userDefinedGroupItems);
            }

            return await Task.FromResult(curActivityDO);
        }

        private Crate CreateDocusignTemplateConfigurationControls(ActivityDO curActivityDO)
        {
            var fieldSelectDocusignTemplateDTO = new DropDownList()
            {
                Label = "Use DocuSign Template",
                Name = "target_docusign_template",
                Required = true,
                Events = new List<ControlEvent>()
                {
                     new ControlEvent("onChange", "requestConfig")
                },
                Source = new FieldSourceDTO
                {
                    Label = "Available Templates",
                    ManifestType = MT.FieldDescription.GetEnumDisplayName()
                }
            };

            var fieldsDTO = new List<ControlDefinitionDTO>()
            {
                fieldSelectDocusignTemplateDTO,
                new TextSource("Email Address", "Upstream Terminal-Provided Fields", "Recipient")
                {
                    selectedKey = "Recipient",
                    ValueSource = "upstream"
                },
               new TextSource("Recipient Name", "Upstream Terminal-Provided Fields", "RecipientName")
                {
                    selectedKey = "RecipientName",
                    ValueSource = "upstream"
                }
            };

            var controls = new StandardConfigurationControlsCM()
            {
                Controls = fieldsDTO
            };

            return CrateManager.CreateStandardConfigurationControlsCrate("Configuration_Controls", fieldsDTO.ToArray());
        }

        public async Task UpdateUpstreamCrate(ActivityDO curActivityDO, IUpdatableCrateStorage updater)
        {
            // Build a crate with the list of available upstream fields
            var curUpstreamFieldsCrate = updater.SingleOrDefault(c => c.ManifestType.Id == (int)MT.FieldDescription
                                                                                && c.Label == "Upstream Terminal-Provided Fields");

            if (curUpstreamFieldsCrate != null)
            {
                updater.Remove(curUpstreamFieldsCrate);
            }

            var curUpstreamFields = (await GetDesignTimeFields(curActivityDO.Id, CrateDirection.Upstream))
                .Fields
                .Where(a => a.Availability == AvailabilityType.RunTime)
                .ToArray();

            //make fields inaccessible to up/downstanding actions
            curUpstreamFields.ToList().ForEach(a => a.Availability = AvailabilityType.Configuration);

            curUpstreamFieldsCrate = CrateManager.CreateDesignTimeFieldsCrate("Upstream Terminal-Provided Fields", curUpstreamFields);
            updater.Add(curUpstreamFieldsCrate);
        }
    }
}
