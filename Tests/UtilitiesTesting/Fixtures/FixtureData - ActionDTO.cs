﻿using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UtilitiesTesting.Fixtures
{
    partial class FixtureData
    {
        public static ActionDTO TestActionDTO1()
        {
            return new ActionDTO()
            {
                Name = "test action type",
                ActivityTemplate = FixtureData.TestActivityTemplateDTO1(),
            };
        }
        public static ActionDTO TestActionDTO2()
        {
            ActionDTO curActionDTO = new ActionDTO()
            {
                Name = "test action type",
                ActivityTemplate = FixtureData.TestActivityTemplateDTO1(),
            };
            curActionDTO.CrateStorage.CrateDTO.Add(CreateStandardConfigurationControls());

            return curActionDTO;
        }

        public static ActionDTO TestActionDTO3()
        {
            ActionDTO curActionDTO = new ActionDTO()
            {
                Name = "test action type",
                ActivityTemplate = FixtureData.TestActivityTemplateDTO1()
            };
            curActionDTO.CrateStorage.CrateDTO.Add(CreateStandardConfigurationControls());
            var configurationFields = JsonConvert.DeserializeObject<StandardConfigurationControlsCM>(curActionDTO.CrateStorage.CrateDTO[0].Contents);

            curActionDTO.CrateStorage.CrateDTO.Add(CreateEventSubscriptionCrate(configurationFields));

            return curActionDTO;
        }

        public static ActionDTO CreateStandardDesignTimeFields()
        {
            ActionDTO curActionDTO = new ActionDTO();
            List<CrateDTO> curCratesDTO = FixtureData.TestCrateDTO2();
            curActionDTO.CrateStorage.CrateDTO.AddRange(curCratesDTO);
            return curActionDTO;
        }

        public static ActionDTO TestActionDTOForSalesforce()
        {
            return new ActionDTO()
            {
                Name = "test salesforce action",
                ActivityTemplate = FixtureData.TestActivityTemplateSalesforce()
            };
        }

        public static ActionDTO TestActionDTOForSendGrid()
        {
            return new ActionDTO()
            {
                Name = "SendEmailViaSendGrid",
                ActivityTemplate = FixtureData.TestActivityTemplateSendGrid()
            };
        }
        public static ActionDTO TestActionDTOSelectFr8ObjectInitial()
        {
            ActionDTO curActionDTO = new ActionDTO()
            {
                Name = "test action type",
                ActivityTemplate = FixtureData.ActivityTemplateDTOSelectFr8Object(),
            };
            // curActionDTO.CrateStorage.CrateDTO.Add(CreateStandardConfigurationControls());

            return curActionDTO;
        }
        public static ActionDTO TestActionDTOSelectFr8ObjectFollowup(string selected)
        {
            ActionDTO curActionDTO = new ActionDTO()
            {
                Name = "test action type",
                ActivityTemplate = FixtureData.ActivityTemplateDTOSelectFr8Object(),
            };
            curActionDTO.CrateStorage.CrateDTO.Add(CreateStandardConfigurationControlSelectFr8Object(selected));

            return curActionDTO;
        }
		
    }
}