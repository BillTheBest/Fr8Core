﻿using System.Collections.Generic;
using Data.Crates;
using StructureMap;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Interfaces;
using Hub.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UtilitiesTesting.Fixtures
{
    public partial class FixtureData
    {
        public static ActivityTemplateDO TestActivityTemplate1()
        {
            ActivityTemplateDO activityTemplateDo = new ActivityTemplateDO
            {
                Id = 1,
                Name = "Write to Sql Server",
                Terminal = TerminalOne(),
                Version = "v3"
            };
            return activityTemplateDo;
        }

        public static ActivityTemplateDO TestActivityTemplate2()
        {
            ActivityTemplateDO activityTemplateDo = new ActivityTemplateDO
            {
                Id = 1,
                Version = "v4"
            };
            return activityTemplateDo;
        }
        public static ActivityTemplateDO TestActivityTemplateDO1()
        {
            var curActivityDO = new ActivityTemplateDO
            {
                Id = 1,
                Name = "Type1",
                Terminal = TerminalTwo(),
                Version = "1"
            };
            return curActivityDO;
        }

        public static ActivityTemplateDO TestActivityTemplateV2()
        {
            var curActionTemplate = new ActivityTemplateDO
            {
                Id = 1,
                Name = "terminal_azure_sql_server",
                Terminal = TerminalThree(),
                Version = "1"
            };

            return curActionTemplate;
        }

        public static ActivityTemplateDTO TestActivityTemplateDTOV2()
        {
            var curActionTemplate = new ActivityTemplateDTO
            {
                Id = 1,
                Name = "terminal_azure_sql_server",
                Version = "1",
                TerminalId = 1
            };

            return curActionTemplate;
        }

        public static ICrateStorage TestConfigurationSettings()
        {
            return FixtureData.CrateStorageDTO();
        }

        public static ICrateStorage TestConfigurationStore()
        {
            return new CrateStorage
            {
                Crate.FromJson("SQL Connection String", JsonConvert.DeserializeObject<JToken>("{type: 'textField', name: 'connection_string', required: true, fieldLabel: 'SQL Connection String'}")),
                Crate.FromJson("SQL Connection String", JsonConvert.DeserializeObject<JToken>("{type: 'textField', name: 'connection_string', required: true, fieldLabel: 'SQL Connection String'}")),
                Crate.FromJson("Custom SQL Query", JsonConvert.DeserializeObject<JToken>("{type: 'textField', name: 'query', required: true, fieldLabel: 'Custom SQL Query'}")),
                Crate.FromJson("Log All Transactions", JsonConvert.DeserializeObject<JToken>("{type: 'checkboxField', name: 'log_transactions', required: true, fieldLabel: 'Log All Transactions?'}")),
                Crate.FromJson("Log Some Transactions", JsonConvert.DeserializeObject<JToken>("{type: 'checkboxField', name: 'log_transactions1', required: true, fieldLabel: 'Log Some Transactions?'}")),
                Crate.FromJson("Log No Transactions", JsonConvert.DeserializeObject<JToken>("{type: 'checkboxField', name: 'log_transactions2', required: true, fieldLabel: 'Log No Transactions?'}")),
                Crate.FromJson("Log Failed Transactions", JsonConvert.DeserializeObject<JToken>("{type: 'checkboxField', name: 'log_transactions3', required: true, fieldLabel: 'Log Failed Transactions?'}"))
            };
        }

        public static ActivityTemplateDO TestActivityTemplateDO2()
        {
            var curActivityDO = new ActivityTemplateDO
            {
                Id = 1,
                Name = "Write to SQL",
                Terminal = TerminalFour(),
                Version = "1"
            };

            return curActivityDO;
        }
    }
}
