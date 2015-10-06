﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using StructureMap;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.ManifestSchemas;
using pluginAzureSqlServer.Interfaces;

namespace pluginAzureSqlServer.Services
{
    public class Event : IEvent
    {
        private readonly Core.Interfaces.ICrate _crate;

        public Event()
        {
            _crate = ObjectFactory.GetInstance<Core.Interfaces.ICrate>();
        }

        public void Process(string externalEventPayload)
        {
            if (string.IsNullOrEmpty(externalEventPayload))
            {
                return;
            }

            var payloadFields = ParseSlackPayloadData(externalEventPayload);

            var slackToken = payloadFields.FirstOrDefault(x => x.Key == "token");
            if (slackToken == null || string.IsNullOrEmpty(slackToken.Value))
            {
                return;
            }

            var eventReportContent = new EventReportMS
            {
                EventNames = "Slack Outgoing Message",
                ProcessDOId = "",
                EventPayload = WrapPayloadDataCrate(payloadFields),
                ExternalAccountId = slackToken.Value
            };

            var curEventReport = _crate.Create(
                "Standard Event Report",
                JsonConvert.SerializeObject(eventReportContent),
                "Standard Event Report",
                7);
            
            var url = Regex.Match(ConfigurationManager.AppSettings["EventWebServerUrl"], @"(\w+://\w+:\d+)").Value + "/dockyard_events";
            new HttpClient().PostAsJsonAsync(new Uri(url, UriKind.Absolute), curEventReport);
        }

        private List<FieldDTO> ParseSlackPayloadData(string message)
        {
            var tokens = message.Split(
                new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            var payloadFields = new List<FieldDTO>();
            foreach (var token in tokens)
            {
                var nameValue = token.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (nameValue.Length < 2)
                {
                    continue;
                }

                var name = HttpUtility.UrlDecode(nameValue[0]);
                var value = HttpUtility.UrlDecode(nameValue[1]);

                payloadFields.Add(new FieldDTO()
                {
                    Key = name,
                    Value = value
                });
            }

            return payloadFields;
        }

        private List<CrateDTO> WrapPayloadDataCrate(List<FieldDTO> payloadFields)
        {
            var fieldsCrate = _crate.Create("Payload Data",
                JsonConvert.SerializeObject(payloadFields));

            return new List<CrateDTO>() { fieldsCrate };
        }
    }
}