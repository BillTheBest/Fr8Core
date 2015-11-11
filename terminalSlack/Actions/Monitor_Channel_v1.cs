﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Crates;
using Hub.Managers;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using TerminalBase.Infrastructure;
using terminalSlack.Interfaces;
using terminalSlack.Services;
using TerminalBase.BaseClasses;

namespace terminalSlack.Actions
{
    public class Monitor_Channel_v1 : BasePluginAction
    {
        private readonly ISlackIntegration _slackIntegration;

        public Monitor_Channel_v1()
        {
            _slackIntegration = new SlackIntegration();
        }

        public async Task<PayloadDTO> Run(ActionDTO actionDto)
        {
            if (NeedsAuthentication(actionDto))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            var processPayload = await GetProcessPayload(actionDto.ProcessId);
            var payloadFields = ExtractPayloadFields(processPayload);

            var payloadChannelIdField = payloadFields.FirstOrDefault(x => x.Key == "channel_id");
            if (payloadChannelIdField == null)
            {
                throw new ApplicationException("No channel_id field found in payload.");
            }

            var payloadChannelId = payloadChannelIdField.Value;
            var actionChannelId = ExtractControlFieldValue(actionDto, "Selected_Slack_Channel");

            if (payloadChannelId != actionChannelId)
            {
                throw new ApplicationException("Unexpected channel-id.");
            }

            using (var updater = Crate.UpdateStorage(processPayload))
            {
                updater.CrateStorage.Add(Data.Crates.Crate.FromContent("Slack Payload Data", new StandardPayloadDataCM(payloadFields)));
            }

            return processPayload;
        }

        private List<FieldDTO> ExtractPayloadFields(PayloadDTO processPayload)
        {
            var eventReportMS = Crate.GetStorage(processPayload).CrateContentsOfType<EventReportCM>().SingleOrDefault();
            if (eventReportMS == null)
            {
                throw new ApplicationException("EventReportCrate is empty.");
            }

            var eventFieldsCrate = eventReportMS.EventPayload.SingleOrDefault();
            if (eventFieldsCrate == null)
            {
                throw new ApplicationException("EventReportMS.EventPayload is empty.");
            }

            return eventReportMS.EventPayload.CrateContentsOfType<StandardPayloadDataCM>().SelectMany(x => x.AllValues()).ToList();
        }

        public override async Task<ActionDTO> Configure(ActionDTO curActionDTO)
        {
            if (NeedsAuthentication(curActionDTO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            return await ProcessConfigurationRequest(curActionDTO, x => ConfigurationEvaluator(x));
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActionDTO curActionDTO)
        {
            if (Crate.IsEmptyStorage(curActionDTO.CrateStorage))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected override async Task<ActionDTO> InitialConfigurationResponse(ActionDTO curActionDTO)
        {
            var oauthToken = curActionDTO.AuthToken.Token;
            var channels = await _slackIntegration.GetChannelList(oauthToken);

            var crateDesignTimeFields = CreateDesignTimeFieldsCrate();
            var crateAvailableChannels = CreateAvailableChannelsCrate(channels);
            var crateEventSubscriptions = CreateEventSubscriptionCrate();

            using (var updater = Crate.UpdateStorage(curActionDTO))
            {
                updater.CrateStorage.Clear();
                PackConfigurationControls(updater.CrateStorage);
                updater.CrateStorage.Add(crateDesignTimeFields);
                updater.CrateStorage.Add(crateAvailableChannels);
                updater.CrateStorage.Add(crateEventSubscriptions);
            }

            return await Task.FromResult<ActionDTO>(curActionDTO);
        }

        private void PackConfigurationControls(CrateStorage crateStorage)
        {
            AddControl(
                crateStorage,
                new DropDownListControlDefinitionDTO()
                {
                    Label = "Select Slack Channel",
                    Name = "Selected_Slack_Channel",
                    Required = true,
                    Events = new List<ControlEvent>()
                    {
                        new ControlEvent("onChange", "requestConfig")
                    },
                    Source = new FieldSourceDTO
                    {
                        Label = "Available Channels",
                        ManifestType = CrateManifestTypes.StandardDesignTimeFields
                    }
                });

            AddControl(
                crateStorage,
                new TextBlockControlDefinitionDTO()
                {
                    Name = "Info_Label",
                    Value = "Slack doesn't currently offer a way for us to automatically request events for this channel. You can do it manually here. use the following values: URL: <strong>http://www.fr8.company/events?dockyard_plugin=terminalSlack&version=1.0</strong>"
                });
        }

        private Crate CreateDesignTimeFieldsCrate()
        {
            var fields = new List<FieldDTO>()
            {
                new FieldDTO() { Key = "token", Value = "token" },
                new FieldDTO() { Key = "team_id", Value = "team_id" },
                new FieldDTO() { Key = "team_domain", Value = "team_domain" },
                new FieldDTO() { Key = "service_id", Value = "service_id" },
                new FieldDTO() { Key = "timestamp", Value = "timestamp" },
                new FieldDTO() { Key = "channel_id", Value = "channel_id" },
                new FieldDTO() { Key = "channel_name", Value = "channel_name" },
                new FieldDTO() { Key = "user_id", Value = "user_id" },
                new FieldDTO() { Key = "user_name", Value = "user_name" },
                new FieldDTO() { Key = "text", Value = "text" }
            };

            var crate =
                Crate.CreateDesignTimeFieldsCrate(
                    "Available Fields",
                    fields.ToArray()
                );

            return crate;
        }

        private Crate CreateAvailableChannelsCrate(IEnumerable<FieldDTO> channels)
        {
            var crate =
                Crate.CreateDesignTimeFieldsCrate(
                    "Available Channels",
                    channels.ToArray()
                );

            return crate;
        }

        private Crate CreateEventSubscriptionCrate()
        {
            var subscriptions = new string[] {
                "Slack Outgoing Message"
            };

            return Crate.CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                subscriptions.ToArray()
                );
        }
    }
}