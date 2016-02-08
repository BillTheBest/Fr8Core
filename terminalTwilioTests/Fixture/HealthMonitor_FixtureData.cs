﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Managers;

namespace terminalTwilioTests.Fixture
{
    class HealthMonitor_FixtureData
    {
        protected ICrateManager CrateManager;
        public HealthMonitor_FixtureData()
        {

            CrateManager = new CrateManager();
        }
        public static ActivityTemplateDTO Send_Via_Twilio_v1_ActivityTemplate()
        {
            return new ActivityTemplateDTO()
            {
                Id = 1,
                Name = "Send_Via_Twilio_TEST",
                Version = "1"
            };
        }
        public static Fr8DataDTO Send_Via_Twilio_v1_InitialConfiguration_Fr8DataDTO()
        {
            var activityTemplate = Send_Via_Twilio_v1_ActivityTemplate();

            var activityDTO = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Name = "Send_Via_Twilio",
                Label = "Send Via Twilio",
                AuthToken = null,
                ActivityTemplate = activityTemplate,
                ActivityTemplateId = activityTemplate.Id
            };

            return new Fr8DataDTO { ActivityDTO = activityDTO };
        }
        public ActivityDTO Send_Via_Twilio_v1_Preconfigured_Crate_With_No_SMS_Number()
        {
            var dataDTO = Send_Via_Twilio_v1_InitialConfiguration_Fr8DataDTO();
            using (var updater = CrateManager.UpdateStorage(dataDTO.ActivityDTO))
            {
                var curCrate = No_SMS_Number_Controls();
                updater.CrateStorage.Add(curCrate);
            }
            return dataDTO.ActivityDTO;
        }
        public Crate No_SMS_Number_Controls()
        {
            var fieldsDTO = new List<ControlDefinitionDTO>()
            {
                new TextSource()
                {
                    UpstreamSourceLabel = "Upstream Terminal-Provided Fields",
                    InitialLabel = "SMS Number",
                    Name = "SMS_Number",
                    Value = "15005550006",
                    Label = "SMS Number",
                },
                new TextSource()
                {
                    UpstreamSourceLabel = "Upstream Terminal-Provided Fields",
                    InitialLabel = "SMS Body",
                    Name = "SMS_Body",
                    Value = "That is the message that we are sending"
                    Label = "SMS Body"
                }
            };
            return CrateManager.CreateStandardConfigurationControlsCrate("Configuration_Controls", fieldsDTO.ToArray());
        }

        public TextSource GetUpstreamCrate()
        {
            return new TextSource
            {
                UpstreamSourceLabel = "Upsteam Crate",
                InitialLabel = "SMS Number",
                ValueSource = "specific",
                Name = "SMS_Number",
                Value = "+15005550006",
                Label = "SMS Number",
                Source = new FieldSourceDTO()
                {
                    Label = "SMS Number",
                    
                },
                Selected = true
            };
        }
    }
}
