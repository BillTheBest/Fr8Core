﻿using System;
using System.Collections.Generic;
using System.Linq;
using Data.Constants;
using Data.Control;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using HealthMonitor.Utility;
using Hub.Managers;
using NUnit.Framework;

namespace terminalFr8CoreTests.Integration
{
	/// <summary>
	/// Mark test case class with [Explicit] attiribute.
	/// It prevents test case from running when CI is building the solution,
	/// but allows to trigger that class from HealthMonitor.
	/// </summary>
	[Explicit]
    public class SetDelay_v1_Tests : BaseTerminalIntegrationTest
    {
		public override string TerminalName
		{
			get { return "terminalFr8Core"; }
		}

		[Test]
		public void Check_Initial_Configuration_Crate_Structure()
		{
			var configureUrl = GetTerminalConfigureUrl();

			var requestActionDTO = CreateRequestActionFixture();
            var dataDTO = new Fr8DataDTO { ActivityDTO = requestActionDTO };
			var responseActionDTO = HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO).Result;

			Assert.NotNull(responseActionDTO);
			Assert.NotNull(responseActionDTO.CrateStorage);
			Assert.NotNull(responseActionDTO.CrateStorage.Crates);

			var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);

			Assert.AreEqual(1, crateStorage.Count);

			Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(x => x.Label == "Configuration_Controls"));

			var configCrate = crateStorage
				.CrateContentsOfType<StandardConfigurationControlsCM>(x => x.Label == "Configuration_Controls")
				.SingleOrDefault();

			ValidateConfigurationCrateStructure(configCrate);
		}

        /// <summary>
        /// Set Delay action does nothing on follow up configuration
        /// </summary>
		[Test]
		public void Check_FollowUp_Configuration_Crate_Structure()
		{
			var configureUrl = GetTerminalConfigureUrl();

			var requestActionDTO = CreateRequestActionFixture();
            var dataDTO = new Fr8DataDTO { ActivityDTO = requestActionDTO };
            var responseActionDTO = HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO).Result;
            dataDTO.ActivityDTO = responseActionDTO;
            SetDuration(responseActionDTO);

			responseActionDTO = HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO).Result;

			Assert.NotNull(responseActionDTO);
			Assert.NotNull(responseActionDTO.CrateStorage);
			Assert.NotNull(responseActionDTO.CrateStorage.Crates);

			var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);

			Assert.AreEqual(1, crateStorage.Count);

			Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(x => x.Label == "Configuration_Controls"));

			var configCrate = crateStorage
				.CrateContentsOfType<StandardConfigurationControlsCM>(x => x.Label == "Configuration_Controls")
				.SingleOrDefault();

			ValidateConfigurationCrateStructure(configCrate);

			var configurationControl = (Duration)configCrate.Controls.FirstOrDefault();

			Assert.AreEqual(0, configurationControl.Days);
            Assert.AreEqual(0, configurationControl.Hours);
            Assert.AreEqual(2, configurationControl.Minutes);
		}

		[Test]
		public void Run_With_Route_Payload_Initial_Run()
		{
			var configureUrl = GetTerminalConfigureUrl();
			var requestActionDTO = CreateRequestActionFixture();

            var dataDTO = new Fr8DataDTO { ActivityDTO = requestActionDTO };

            var responseActionDTO = HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO).Result;
			SetDuration(responseActionDTO);
			var runUrl = GetTerminalRunUrl();
		    dataDTO.ActivityDTO = responseActionDTO;
            AddOperationalStateCrate(dataDTO, new OperationalStateCM());
            var runResponse = HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO).Result;
			Assert.NotNull(runResponse);
		    var crateStorage = Crate.GetStorage(runResponse);
            var operationalStateCrate = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
            Assert.AreEqual(ActivityResponse.RequestSuspend.ToString(), operationalStateCrate.CurrentActivityResponse.Type);
		}

        [Test]
        public void Run_With_Route_Payload_Second_Run()
        {
            var configureUrl = GetTerminalConfigureUrl();
            var requestActionDTO = CreateRequestActionFixture();
            var dataDTO = new Fr8DataDTO { ActivityDTO = requestActionDTO };

            dataDTO.ActivityDTO = HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO).Result;
            SetDuration(dataDTO.ActivityDTO);
            var runUrl = GetTerminalRunUrl();
            var operationalState = new OperationalStateCM
            {
                CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.RequestSuspend)
            };
            AddOperationalStateCrate(dataDTO, operationalState);

            var runResponse = HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO).Result;
            Assert.NotNull(runResponse);
            var crateStorage = Crate.GetStorage(runResponse);
            var operationalStateCrate = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
            Assert.AreEqual(ActivityResponse.Success.ToString(), operationalStateCrate.CurrentActivityResponse.Type);
        }
        
		private ActivityTemplateDTO CreateActivityTemplateFixture()
		{
			var activityTemplate = new ActivityTemplateDTO
			{
				Id = 1,
				Name = "SetDelay_TEST",
				Version = "1"
			};

			return activityTemplate;
		}

		private ActivityDTO CreateRequestActionFixture()
		{
			var activityTemplate = CreateActivityTemplateFixture();

			var requestActionDTO = new ActivityDTO
			{
				Id = Guid.NewGuid(),
				Name = "SetDelay",
				Label = "Set Delay",
				ActivityTemplate = activityTemplate,
				AuthToken = null
			};

			return requestActionDTO;
		}

		private void SetDuration(ActivityDTO responseActionDTO)
		{
			using (var updater = Crate.UpdateStorage(responseActionDTO))
			{
				var controls = updater.CrateStorage
					.CrateContentsOfType<StandardConfigurationControlsCM>()
					.Single();

				var duration = (Duration)controls.Controls[0];
			    duration.Days = 0;
                duration.Hours = 0;
                duration.Minutes = 2;
			}
		}


		private void ValidateConfigurationCrateStructure(StandardConfigurationControlsCM configCrate)
		{
			var controls = configCrate.Controls;

			Assert.AreEqual(1, controls.Count);

			var configurationControl = controls.FirstOrDefault();

			Assert.IsInstanceOf<Duration>(configurationControl);
            Assert.AreEqual("Delay_Duration", configurationControl.Name);
            Assert.AreEqual("Please enter delay duration", configurationControl.Label);

			var configurationControlEvents = configurationControl.Events;

			Assert.IsNull(configurationControlEvents);
            Assert.IsNull(configurationControl.Source);
		}
	}
}