﻿using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using terminalGoogle.Infrastructure;
using Utilities.Configuration.Azure;
using Hub.Managers;
using Data.Crates;
using System.Threading.Tasks;
namespace terminalGoogle.Services
{
    public class Event : IEvent
    {
        private readonly ICrateManager _crate;

        public Event()
        {
            _crate = ObjectFactory.GetInstance<ICrateManager>();
        }

        public async Task<Crate> Process(string externalEventPayload)
        {
            if (string.IsNullOrEmpty(externalEventPayload))
            {
                return null;
            }

            var payloadFields = ParseGoogleFormPayloadData(externalEventPayload);

            var externalAccountId = payloadFields.FirstOrDefault(x => x.Key == "user_id");
            if (externalAccountId == null || string.IsNullOrEmpty(externalAccountId.Value))
            {
                return null;
            }

            var eventReportContent = new EventReportCM
            {
                EventNames = "Google Form Response",
                ContainerDoId = "",
                EventPayload = WrapPayloadDataCrate(payloadFields),
                ExternalAccountId = externalAccountId.Value,
                Manufacturer = "Google"
            };

            //prepare the event report
            var curEventReport = Crate.FromContent("Standard Event Report", eventReportContent);

            return curEventReport;
        }

        private List<FieldDTO> ParseGoogleFormPayloadData(string message)
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

        private ICrateStorage WrapPayloadDataCrate(List<FieldDTO> payloadFields)
        {

            return new CrateStorage(Data.Crates.Crate.FromContent("Payload Data", new StandardPayloadDataCM(payloadFields)));
        }
    }
}