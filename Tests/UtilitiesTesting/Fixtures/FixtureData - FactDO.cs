﻿using Data.Entities;
using Data.States;
using Data.Wrappers;
using DocuSign.Integrations.Client;
using System;

namespace UtilitiesTesting.Fixtures
{
    partial class FixtureData
    {
        public static FactDO TestFactDO()
        {
            var curFactDO = new FactDO
            {
                ObjectId = "Plugin Incident",
                CustomerId = "not_applicable",
                Data = "service_start_up",
                PrimaryCategory = "Operations",
                SecondaryCategory = "System Startup",
                Activity = "system startup",
                CreateDate = DateTimeOffset.Now.AddDays(-1) 
            };
            return curFactDO;
        }
    
    }
}