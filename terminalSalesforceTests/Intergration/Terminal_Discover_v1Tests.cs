﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using NUnit.Framework;

namespace terminalSalesforceTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    public class Terminal_Discover_v1Tests : BaseTerminalIntegrationTest
    {
        private const int ActivityCount = 5;
        private const string Create_Account_Activity_Name = "Create_Account";
        private const string Create_Contact_Activity_Name = "Create_Contact";
        private const string Create_Lead_Activity_Name = "Create_Lead";
        private const string Get_Data_Activity_Name = "Get_Data";
        private const string Post_To_Chatter_Name = "Post_To_Chatter";


        public override string TerminalName
        {
            get { return "terminalSalesforce"; }
        }

        /// <summary>
        /// Validate correct crate-storage structure in initial configuration response.
        /// </summary>
        [Test, CategoryAttribute("Integration.terminalSalesforce")]
        public async Task Terminal_Salesforce_Discover()
        {
            var discoverUrl = GetTerminalDiscoverUrl();

            var terminalDiscoverResponse = await HttpGetAsync<StandardFr8TerminalCM>(discoverUrl);

            Assert.IsNotNull(terminalDiscoverResponse, "Terminal Salesforce discovery did not happen.");
            Assert.IsNotNull(terminalDiscoverResponse.Activities, "Salesforce terminal actions were not loaded");
            Assert.AreEqual(ActivityCount, terminalDiscoverResponse.Activities.Count,
                "Not all terminal Salesforce actions were loaded");
            Assert.AreEqual(terminalDiscoverResponse.Activities.Any(a => a.Name == Create_Account_Activity_Name), true,
                "Action " + Create_Account_Activity_Name + " was not loaded");
            Assert.AreEqual(terminalDiscoverResponse.Activities.Any(a => a.Name == Create_Contact_Activity_Name), true,
                "Action " + Create_Contact_Activity_Name + " was not loaded");
            Assert.AreEqual(terminalDiscoverResponse.Activities.Any(a => a.Name == Create_Lead_Activity_Name), true,
                "Action " + Create_Lead_Activity_Name + " was not loaded");
            Assert.AreEqual(terminalDiscoverResponse.Activities.Any(a => a.Name == Get_Data_Activity_Name), true,
                "Action " + Get_Data_Activity_Name + " was not loaded");
            Assert.AreEqual(terminalDiscoverResponse.Activities.Any(a => a.Name == Post_To_Chatter_Name), true,
                "Action " + Post_To_Chatter_Name + " was not loaded");
        }
    }
}
