﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.ManifestSchemas;
using Newtonsoft.Json;
using StructureMap;
using TerminalBase.Infrastructure;
using terminalDocuSign.Infrastructure;
using TerminalBase.BaseClasses;

namespace terminalDocuSign.Actions
{
    public class Record_DocuSign_Events_v1 : BasePluginAction
    {
        /// <summary>
        /// //For this action, both Initial and Followup configuration requests are same. Hence it returns Initial config request type always.
        /// </summary>
        /// <param name="curActionDTO"></param>
        /// <returns></returns>
        public async Task<ActionDTO> Configure(ActionDTO curActionDTO)
        {
            if (NeedsAuthentication(curActionDTO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            return await ProcessConfigurationRequest(curActionDTO, dto => ConfigurationRequestType.Initial);
        }

        protected override async Task<ActionDTO> InitialConfigurationResponse(ActionDTO curActionDTO)
        {
            /*
             * Discussed with Alexei and it is required to have empty Standard Configuration Control in the crate.
             * So we create a text block which informs the user that this particular aciton does not require any configuration.
             */
            var textBlock = new TextBlockControlDefinitionDTO()
            {
                Label = "Monitor All DocuSign events",
                Value = "This Action doesn't require any configuration.",
                CssClass = "well well-lg"
            };
            var curControlsCrate = PackControlsCrate(textBlock);

            //create a Standard Event Subscription crate
            var curEventSubscriptionsCrate = Crate.CreateStandardEventSubscriptionsCrate("Standard Event Subscription", DocuSignEventNames.GetAllEventNames());

            //create Standard Design Time Fields for Available Run-Time Objects
            var curAvailableRunTimeObjectsDesignTimeCrate =
                Crate.CreateDesignTimeFieldsCrate("Available Run-Time Objects", new FieldDTO[]
                {
                    new FieldDTO {Key = "DocuSign Envelope", Value = string.Empty},
                    new FieldDTO {Key = "DocuSign Event", Value = string.Empty}
                });

            //update crate storage with standard event subscription crate
            curActionDTO.CrateStorage = new CrateStorageDTO()
            {
                CrateDTO = new List<CrateDTO> { curControlsCrate, curEventSubscriptionsCrate, curAvailableRunTimeObjectsDesignTimeCrate }
            };

            /*
             * Note: We should not call Activate at the time of Configuration. For this action, it may be valid use case.
             * Because this particular action will be used internally, it would be easy to execute the Process directly.
             */
            await Activate(curActionDTO);

            return await Task.FromResult(curActionDTO);
        }

        public Task<object> Activate(ActionDTO curActionDTO)
        {
            DocuSignAccount curDocuSignAccount = new DocuSignAccount();
            var curConnectProfile = curDocuSignAccount.GetDocuSignConnectProfiles();

            if (curConnectProfile.configurations != null &&
                !curConnectProfile.configurations.Any(config => !string.IsNullOrEmpty(config.name) && config.name.Equals("MonitorAllDocuSignEvents")))
            {
                var monitorConnectConfiguration = new DocuSign.Integrations.Client.Configuration
                {
                    allowEnvelopePublish = "true",
                    allUsers = "true",
                    enableLog = "true",
                    requiresAcknowledgement = "true",
                    envelopeEvents = string.Join(",", DocuSignEventNames.GetEventsFor("Envelope")),
                    recipientEvents = string.Join(",", DocuSignEventNames.GetEventsFor("Recipient")),
                    name = "MonitorAllDocuSignEvents",
                    urlToPublishTo =
                        Regex.Match(ConfigurationManager.AppSettings["EventWebServerUrl"], @"(\w+://\w+:\d+)").Value +
                        "/events?dockyard_plugin=terminalDocuSign&version=1"
                };

                curDocuSignAccount.CreateDocuSignConnectProfile(monitorConnectConfiguration);
            }

            return Task.FromResult((object)true);
        }

        public object Deactivate(ActionDTO curDataPackage)
        {
            DocuSignAccount curDocuSignAccount = new DocuSignAccount();
            var curConnectProfile = curDocuSignAccount.GetDocuSignConnectProfiles();

            if (Int32.Parse(curConnectProfile.totalRecords) > 0 && curConnectProfile.configurations.Any(config => config.name.Equals("MonitorAllDocuSignEvents")))
            {
                curDocuSignAccount.DeleteDocuSignConnectProfile("MonitorAllDocuSignEvents");
            }

            return true;
        }

        public async Task<PayloadDTO> Run(ActionDTO actionDto)
        {
            if (NeedsAuthentication(actionDto))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            var curProcessPayload = await GetProcessPayload(actionDto.ProcessId);

            var curEventReport = JsonConvert.DeserializeObject<EventReportCM>(curProcessPayload.CrateStorageDTO().CrateDTO[0].Contents);

            if (curEventReport.EventNames.Contains("Envelope"))
            {
                using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    IList<KeyValuePair<string, string>> docuSignFields =
                        Crate.GetContents<List<KeyValuePair<string, string>>>(curEventReport.EventPayload[0]);

                    DocuSignEnvelopeCM envelope = new DocuSignEnvelopeCM
                    {
                        CompletedDate = docuSignFields.First(field => field.Key.Equals("CompletedDate")).Value,
                        CreateDate = docuSignFields.First(field => field.Key.Equals("CreateDate")).Value,
                        DeliveredDate = docuSignFields.First(field => field.Key.Equals("DeliveredDate")).Value,
                        EnvelopeId = docuSignFields.First(field => field.Key.Equals("EnvelopeId")).Value,
                        ExternalAccountId = docuSignFields.First(field => field.Key.Equals("Email")).Value,
                        SentDate = docuSignFields.First(field => field.Key.Equals("SentDate")).Value,
                        Status = docuSignFields.First(field => field.Key.Equals("Status")).Value
                    };

                    DocuSignEventCM events = new DocuSignEventCM
                    {
                        EnvelopeId = docuSignFields.First(field => field.Key.Equals("EnvelopeId")).Value,
                        EventId = docuSignFields.First(field => field.Key.Equals("EventId")).Value,
                        Object = docuSignFields.First(field => field.Key.Equals("Object")).Value,
                        RecepientId = docuSignFields.First(field => field.Key.Equals("RecipientId")).Value,
                        Status = docuSignFields.First(field => field.Key.Equals("Status")).Value,
                        ExternalAccountId = docuSignFields.First(field => field.Key.Equals("Email")).Value
                    };

                    curProcessPayload.UpdateCrateStorageDTO(new List<CrateDTO>
                    {
                        Crate.Create("DocuSign Envelope Manifest", JsonConvert.SerializeObject(envelope), envelope.ManifestName, envelope.ManifestId),
                        Crate.Create("DocuSign Event Manifest", JsonConvert.SerializeObject(events), events.ManifestName, events.ManifestId)
                    });
                }
            }

            return curProcessPayload;
        }
    }
}