﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using HealthMonitor.Utility;
using Hub.Managers;
using NUnit.Framework;
using terminalFr8Core.Actions;
using UtilitiesTesting.Fixtures;

namespace terminaBaselTests.Tools.Activities
{
    public class IntegrationTestTools_terminalFr8
    {
        private readonly BaseHubIntegrationTest _baseHubITest;

        public IntegrationTestTools_terminalFr8(BaseHubIntegrationTest baseHubIntegrationTest)
        {
            _baseHubITest = baseHubIntegrationTest;
        }

        public async Task<ActivityDTO> AddAndConfigureBuildMessage(PlanDTO plan, int ordering, string messageName, string messageBodyTemplate)
        {
            var activityName = "Build_Message";
            var buildMessageActivityDTO = FixtureData.Build_Message_v1_InitialConfiguration();

            var activityCategoryParam = new ActivityCategory[] { ActivityCategory.Processors };
            var activityTemplates = await _baseHubITest.HttpPostAsync<ActivityCategory[], List<WebServiceActivitySetDTO>>(_baseHubITest.GetHubApiBaseUrl() + "webservices/activities", activityCategoryParam);
            var apmActivityTemplate = activityTemplates.SelectMany(a => a.Activities).Single(a => a.Name == activityName);

            buildMessageActivityDTO.ActivityTemplate = apmActivityTemplate;

            //connect current activity with a plan
            var subPlan = plan.Plan.SubPlans.FirstOrDefault();
            buildMessageActivityDTO.ParentPlanNodeId = subPlan.SubPlanId;
            buildMessageActivityDTO.RootPlanNodeId = plan.Plan.Id;
            buildMessageActivityDTO.Ordering = ordering;

            //call initial configuration to server
            buildMessageActivityDTO = await _baseHubITest.HttpPostAsync<ActivityDTO, ActivityDTO>(_baseHubITest.GetHubApiBaseUrl() + "activities/save", buildMessageActivityDTO);
            //this call is without authtoken
            buildMessageActivityDTO = await _baseHubITest.HttpPostAsync<ActivityDTO, ActivityDTO>(_baseHubITest.GetHubApiBaseUrl() + "activities/configure", buildMessageActivityDTO);
            //call followup configuration
            using (var crateStorage = _baseHubITest.Crate.GetUpdatableStorage(buildMessageActivityDTO))
            {
                var controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
                Assert.IsNotNull(controlsCrate, $"{activityName}: Crate StandardConfigurationControlsCM is missing in API response");
                var activityUi = new Build_Message_v1.ActivityUi();
                activityUi.SyncWith(controlsCrate.Content);
                crateStorage.Remove<StandardConfigurationControlsCM>();
                activityUi.Name.Value = messageName;
                activityUi.Body.Value = messageBodyTemplate;
                crateStorage.Add(Crate<StandardConfigurationControlsCM>.FromContent(controlsCrate.Label, new StandardConfigurationControlsCM(activityUi.Controls.ToArray()), controlsCrate.Availability));
            }
            buildMessageActivityDTO = await _baseHubITest.HttpPostAsync<ActivityDTO, ActivityDTO>(_baseHubITest.GetHubApiBaseUrl() + "activities/save", buildMessageActivityDTO);
            buildMessageActivityDTO = await _baseHubITest.HttpPostAsync<ActivityDTO, ActivityDTO>(_baseHubITest.GetHubApiBaseUrl() + "activities/configure", buildMessageActivityDTO);

            return buildMessageActivityDTO;
        }

        public async Task<ActivityDTO> ConfigureLoopActivity(ActivityDTO activityDTO, string manifestType, string crateDescriptionLabel)
        {
            using (var loopCrateStorage = _baseHubITest.Crate.GetUpdatableStorage(activityDTO))
            {

                var loopControlsCrate = loopCrateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
                var loopControls = loopControlsCrate.Content.Controls;

                var loopCrateChooser = loopControls.SingleOrDefault(x => x.Type == ControlTypes.CrateChooser && x.Name == "Available_Crates") as CrateChooser;
                // Assert Loop activity has CrateChooser with assigned manifest types.
                Assert.NotNull(loopCrateChooser);
                Assert.AreEqual(1, loopCrateChooser.CrateDescriptions.Count);
                Assert.AreEqual(manifestType, loopCrateChooser.CrateDescriptions[0].ManifestType);
                Assert.AreEqual(crateDescriptionLabel, loopCrateChooser.CrateDescriptions[0].Label);

                loopCrateChooser.CrateDescriptions.First(x => x.Label == crateDescriptionLabel && x.ManifestType == manifestType).Selected = true;
            }

            activityDTO = await _baseHubITest.HttpPostAsync<ActivityDTO, ActivityDTO>(_baseHubITest.GetHubApiBaseUrl() + "activities/save", activityDTO);
            activityDTO = await _baseHubITest.HttpPostAsync<ActivityDTO, ActivityDTO>(_baseHubITest.GetHubApiBaseUrl() + "activities/configure", activityDTO);

            return activityDTO;
        }

    }
}
