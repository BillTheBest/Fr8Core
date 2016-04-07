﻿using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerminalBase.BaseClasses;
using terminalGoogle.DataTransferObjects;
using Hub.Managers;
using terminalGoogle.Services;
using TerminalBase.Infrastructure;
using Data.Control;
using Hub.Exceptions;
using Utilities.Configuration.Azure;

namespace terminalGoogle.Actions
{
    public class Monitor_Form_Responses_v1 : BaseTerminalActivity
    {
        private readonly GoogleDrive _googleDrive;
        private readonly GoogleAppScript _googleAppScript;

        public Monitor_Form_Responses_v1()
        {
            _googleDrive = new GoogleDrive();
            _googleAppScript = new GoogleAppScript();
        }

        protected bool NeedsAuthentication(AuthorizationTokenDO authTokenDO)
        {
            if (authTokenDO == null) return true;
            if (!base.NeedsAuthentication(authTokenDO))
                return false;
            var token = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
            // we may also post token to google api to check its validity
            return (token.Expires - DateTime.Now > TimeSpan.FromMinutes(5) ||
                    !string.IsNullOrEmpty(token.RefreshToken));
        }

        public override async Task<ActivityDO> Configure(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            if (CheckAuthentication(curActivityDO, authTokenDO))
            {
                return curActivityDO;
            }

            return await ProcessConfigurationRequest(curActivityDO, ConfigurationEvaluator, authTokenDO);
        }

        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            var authDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);

            using (var storage = CrateManager.GetUpdatableStorage(curActivityDO))
            {


                var ccCrate = storage.CrateContentsOfType<StandardConfigurationControlsCM>().First();
                var control = ccCrate.FindByNameNested<DropDownList>("Selected_Google_Form");

                if (!string.IsNullOrWhiteSpace(control?.Value))
                {

                    //var result = await _googleAppScript.RunScript("M_snhqvaPfe7gMc5XhGu52ZK7araUiK37", "getFoldersUnderRoot", authDTO/*, control.Value*/);
                    int wtf = 0;
                }
            }

            return curActivityDO;
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            if (CrateManager.IsStorageEmpty(curActivityDO))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected override async Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            if (curActivityDO.Id != Guid.Empty)
            {
                var authDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
                var configurationCrate = PackCrate_ConfigurationControls();
                await FillSelectedGoogleFormSource(configurationCrate, "Selected_Google_Form", authDTO);            
                var eventCrate = CreateEventSubscriptionCrate();

                using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
                {
                    crateStorage.Add(configurationCrate);
                    crateStorage.Add(eventCrate);
                }
            }
            else
            {
                throw new ArgumentException(
                    "Configuration requires the submission of an Action that has a real ActionId");
            }
            return await Task.FromResult(curActivityDO);
        }

        private Crate PackCrate_ConfigurationControls()
        {
            var fieldSelectTemplate = new DropDownList()
            {
                Label = "Select Google Form",
                Name = "Selected_Google_Form",
                Required = true,
                Source = null,
                Events = new List<ControlEvent>() {new ControlEvent("onChange", "requestConfig")}
            };

            var controls = PackControlsCrate(fieldSelectTemplate);
            return controls;
        }



        private Crate CreateEventSubscriptionCrate()
        {
            var subscriptions = new string[] {
                "Google Form Response"
            };

            return CrateManager.CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                "Google",
                subscriptions.ToArray()
                );
        }

        public override async Task<ActivityDO> Activate(ActivityDO curActionDTO, AuthorizationTokenDO authTokenDO)
        {
            var googleAuthDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);

            //get form id
            var controlsCrate = CrateManager.GetStorage(curActionDTO).CratesOfType<StandardConfigurationControlsCM>().FirstOrDefault();
            var standardControls = controlsCrate.Get<StandardConfigurationControlsCM>();

            var googleFormControl = standardControls.FindByName("Selected_Google_Form");

            var formId = googleFormControl.Value;

            if (string.IsNullOrEmpty(formId))
                throw new ArgumentNullException("Google Form selected is empty. Please select google form to receive.");

            var scriptUrl = await _googleDrive.CreateFr8TriggerForDocument(googleAuthDTO, formId);
            await HubCommunicator.NotifyUser(new TerminalNotificationDTO
            {
                Type = "Success",
                ActivityName = "Monitor_Form_Responses",
                ActivityVersion = "1",
                TerminalName = "terminalGoogle",
                TerminalVersion = "1",
                Message = "You need to create fr8 trigger on current form please go to this url and run Initialize function manually. Ignore this message if you completed this step before. " + scriptUrl,
                Subject = "Trigger creation URL"
            }, CurrentFr8UserId);

            /*
            var fieldResult = new List<FieldDTO>() { new FieldDTO() { Key = result, Value = result } };
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActionDTO))
            {
                crateStorage.Add(Data.Crates.Crate.FromContent("Google Form Payload Data", new StandardPayloadDataCM(fieldResult)));
            }
            */
            return await Task.FromResult(curActionDTO);
        }


      

        public override async Task<ActivityDO> Deactivate(ActivityDO curActionDTO)
        {
            return await Task.FromResult(curActionDTO);
        }

        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(curActivityDO, containerId);

            if (NeedsAuthentication(authTokenDO))
            {
                return NeedsAuthenticationError(payloadCrates);
            }


            var payloadFields = ExtractPayloadFields(payloadCrates);

            // once we activate the plan we run it. When we run the plan manualy there is no payload with event data. 
            // Just return Success as a quick fix to avoid "Plan Failed" message.
            if (payloadFields == null)
            {
                return Success(payloadCrates);
            }

            var formResponseFields = CreatePayloadFormResponseFields(payloadFields);

            // once we activate the plan we run it. When we run the plan manualy there is no payload with event data. 
            // Just return Success as a quick fix to avoid "Plan Failed" message.
            if (formResponseFields == null)
            {
                return Success(payloadCrates);
            }

            using (var crateStorage = CrateManager.GetUpdatableStorage(payloadCrates))
            {
                crateStorage.Add(Crate.FromContent("Google Form Payload Data", new StandardPayloadDataCM(formResponseFields)));
            }

            return Success(payloadCrates);
        }

        private List<FieldDTO> CreatePayloadFormResponseFields(List<FieldDTO> payloadfields)
        {
            List<FieldDTO> formFieldResponse = new List<FieldDTO>();
            string[] formresponses = payloadfields.FirstOrDefault(w => w.Key == "response").Value.Split(new char[] { '&' });

            if (formresponses.Length > 0)
            {
                formresponses[formresponses.Length - 1] = formresponses[formresponses.Length - 1].TrimEnd(new char[] { '&' });

                foreach (var response in formresponses)
                {
                    string[] itemResponse = response.Split(new char[] { '=' });

                    if (itemResponse.Length >= 2)
                    {
                        formFieldResponse.Add(new FieldDTO() { Key = itemResponse[0], Value = itemResponse[1] });
                    }
                }
            }
            else
            {
                throw new ArgumentNullException("No payload fields extracted");
            }

            return formFieldResponse;
        }

        private List<FieldDTO> ExtractPayloadFields(PayloadDTO payloadCrates)
        {
            var eventReportMS = CrateManager.GetStorage(payloadCrates).CrateContentsOfType<EventReportCM>().SingleOrDefault();
            if (eventReportMS == null)
            {
                return null;
            }

            var eventFieldsCrate = eventReportMS.EventPayload.SingleOrDefault();
            if (eventFieldsCrate == null)
            {
                return null;
            }

            return eventReportMS.EventPayload.CrateContentsOfType<StandardPayloadDataCM>().SelectMany(x => x.AllValues()).ToList();
        }

        #region Fill Source

        private async Task FillSelectedGoogleFormSource(Crate configurationCrate, string controlName, GoogleAuthDTO authDTO)
        {
            var configurationControl = configurationCrate.Get<StandardConfigurationControlsCM>();
            var control = configurationControl.FindByNameNested<DropDownList>(controlName);
            if (control != null)
            {
                control.ListItems = await GetGoogleForms(authDTO);
            }
        }

        private async Task<List<ListItem>> GetGoogleForms(GoogleAuthDTO authDTO)
        {
            if (string.IsNullOrEmpty(authDTO.RefreshToken))
                throw new ArgumentNullException("Token is empty");

            var files = await _googleDrive.GetGoogleForms(authDTO);
            return files.Select(file => new ListItem() { Key = file.Value, Value = file.Key }).ToList();
        }
        #endregion
    }
}