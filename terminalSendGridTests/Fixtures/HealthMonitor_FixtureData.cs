﻿using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace terminalSendGridTests.Fixtures
{
    public class HealthMonitor_FixtureData
    {
        public static ActivityTemplateDTO SendEmailViaSendGrid_v1_ActivityTemplate()
        {
            return new ActivityTemplateDTO()
            {
                Id = 1,
                Name = "SendEmailViaSendGrid",
                Version = "1",
                AuthenticationType = AuthenticationType.None
            };
        }

        public static ActionDTO SendEmailViaSendGrid_v1_InitialConfiguration_ActionDTO()
        {
            var activityTemplate = SendEmailViaSendGrid_v1_ActivityTemplate();

            return new ActionDTO()
            {
                Id = Guid.NewGuid(),
                Name = "SendEmailViaSendGrid",
                Label = "Send Email Via Send Grid",
                AuthToken = null,
                ActivityTemplate = activityTemplate,
                ActivityTemplateId = activityTemplate.Id
            };
        }

    }
}
