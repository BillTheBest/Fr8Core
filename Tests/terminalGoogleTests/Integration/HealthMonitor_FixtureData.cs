﻿using System;
using Data.Interfaces.DataTransferObjects;
using Data.Entities;
using Data.Crates;
using Data.Control;
using Data.Interfaces.Manifests;
using System.Collections.Generic;
using Hub.Managers;
using System.Linq;
using System.Runtime.InteropServices;
using terminalGoogle.Actions;
using terminalGoogle.DataTransferObjects;
using terminalGoogleTests.Integration;

namespace terminalGoogleTests.Unit
{
    public class HealthMonitor_FixtureData
    {

        protected ICrateManager CrateManager;
        public HealthMonitor_FixtureData()
        {

            CrateManager = new CrateManager();
        }

        public static AuthorizationTokenDTO Google_AuthToken()
        {
            return new AuthorizationTokenDTO()
            {
                Token = @"{""AccessToken"":""ya29.PwJez2aHwjGxsxcho6TfaFseWjPbi1ThgINsgiawOKLlzyIgFJHkRdq76YrnuiGT3jhr"",""RefreshToken"":""1/HVhoZXzxFrPyC0JVlbEIF_VOBDm_IhrKoLKnt6QpyFRIgOrJDtdun6zK6XiATCKT"",""Expires"":""2015-12-03T11:12:43.0496208+08:00""}"
            };
        }

        public static AuthorizationTokenDTO Google_AuthToken1()
        {
            return new AuthorizationTokenDTO()
            {
                Token = @"{""AccessToken"":""ya29.OgLf-SvZTHJcdN9tIeNEjsuhIPR4b7KBoxNOuELd0T4qFYEa001kslf31Lme9OQCl6S5"",""RefreshToken"":""1/04H9hNCEo4vfX0nHHEdViZKz1CtesK8ByZ_TOikwVDc"",""Expires"":""2017-11-28T13:29:12.653075+05:00""}"
            };
        }

        public static GoogleAuthDTO NewGoogle_AuthToken_As_GoogleAuthDTO()
        {
            return new GoogleAuthDTO
            {
                AccessToken = "ya29.sAIlmsk843IiMs54TCbaN6XitYsrFa00XcuKvtV75lWuKIWSglzWv_F1MCLHWyuNRg",
                Expires = new DateTime(2017, 03, 19, 0, 0, 0),
                RefreshToken = "1/3DJhIxl_HceJmyZaWwI_O9MRdHyDGCtWo-69dZRbgBQ"
            };
        }

        protected Crate PackControls(StandardConfigurationControlsCM page)
        {
            return PackControlsCrate(page.Controls.ToArray());
        }

        protected Crate<StandardConfigurationControlsCM> PackControlsCrate(params ControlDefinitionDTO[] controlsList)
        {
            return Crate<StandardConfigurationControlsCM>.FromContent("Configuration_Controls", new StandardConfigurationControlsCM(controlsList));
        }

        private Crate PackCrate_GoogleForms()
        {
            Crate crate;

            var curFields = new List<FieldDTO>() { new FieldDTO() { Key = "Survey Form", Value = "1z7mIQdHeFIpxBm92sIFB52B7SwyEO3IT5LiUcmojzn8" } }.ToArray();
            crate = CrateManager.CreateDesignTimeFieldsCrate("Available Forms", curFields);

            return crate;
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

        public static ActivityTemplateDTO Monitor_Form_Responses_v1_ActivityTemplate()
        {
            return new ActivityTemplateDTO()
            {
                Id = 1,
                Name = "Monitor_Form_Responses_TEST",
                Version = "1"
            };
        }

        private Crate PackCrate_ConfigurationControls()
        {
            var fieldSelectTemplate = new DropDownList()
            {
                Label = "Select Google Form",
                Name = "Selected_Google_Form",
                Required = true,
                selectedKey = "Survey Form",
                Value = "1z7mIQdHeFIpxBm92sIFB52B7SwyEO3IT5LiUcmojzn8",
                Source = new FieldSourceDTO
                {
                    Label = "Available Forms",
                    ManifestType = CrateManifestTypes.StandardDesignTimeFields
                }
            };

            var controls = PackControlsCrate(fieldSelectTemplate);
            return controls;
        }

        public void ActivateCrateStorage(ActivityDTO curActivityDO)
        {
            var configurationControlsCrate = PackCrate_ConfigurationControls();
            var crateDesignTimeFields = PackCrate_GoogleForms();
            var eventCrate = CreateEventSubscriptionCrate();

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Add(configurationControlsCrate);
                crateStorage.Add(crateDesignTimeFields);
                crateStorage.Add(eventCrate);
            }
        }

        public static Fr8DataDTO Monitor_Form_Responses_v1_InitialConfiguration_Fr8DataDTO()
        {
            var activityTemplate = Monitor_Form_Responses_v1_ActivityTemplate();

            var activityDTO = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Label = "Monitor Form Responses",
                AuthToken = Google_AuthToken(),
                ActivityTemplate = activityTemplate
            };
            return new Fr8DataDTO { ActivityDTO = activityDTO };
        }

        public Fr8DataDTO Monitor_Form_Responses_v1_ActivateDeactivate_Fr8DataDTO()
        {
            var activityTemplate = Monitor_Form_Responses_v1_ActivityTemplate();

            var activity = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Label = "Monitor Form Responses",
                AuthToken = Google_AuthToken(),
                ActivityTemplate = activityTemplate,
                ParentPlanNodeId = Guid.NewGuid()
            };

            ActivateCrateStorage(activity);
            return new Fr8DataDTO { ActivityDTO = activity };
        }

        private ICrateStorage WrapPayloadDataCrate(List<FieldDTO> payloadFields)
        {
            return new CrateStorage(Data.Crates.Crate.FromContent("Payload Data", new StandardPayloadDataCM(payloadFields)));
        }

        private Crate PayloadRaw()
        {
            List<FieldDTO> payloadFields = new List<FieldDTO>();
            payloadFields.Add(new FieldDTO() { Key = "user_id", Value = "g_admin@dockyard.company" });
            payloadFields.Add(new FieldDTO() { Key = "response", Value = "What is your pets name=cat&What is your favorite book?=book&Who is your favorite superhero?=hero&" });
            var eventReportContent = new EventReportCM
            {
                EventNames = "Google Form Response",
                ContainerDoId = "",
                EventPayload = WrapPayloadDataCrate(payloadFields),
                ExternalAccountId = "g_admin@dockyard.company",
                Manufacturer = "Google"
            };

            //prepare the event report
            var curEventReport = Crate.FromContent("Standard Event Report", eventReportContent);
            return curEventReport;
        }

        private Crate PayloadEmptyRaw()
        {
            List<FieldDTO> payloadFields = new List<FieldDTO>();
            var eventReportContent = new EventReportCM
            {
                EventNames = "Google Form Response",
                ContainerDoId = "",
                EventPayload = WrapPayloadDataCrate(payloadFields),
                ExternalAccountId = "g_admin@dockyard.company",
                Manufacturer = "Google"
            };

            //prepare the event report
            var curEventReport = Crate.FromContent("Standard Event Report", eventReportContent);
            return curEventReport;
        }

        public ActivityDTO Monitor_Form_Responses_v1_Run_ActivityDTO()
        {
            var activityTemplate = Monitor_Form_Responses_v1_ActivityTemplate();

            var activity = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Label = "Monitor Form Responses",
                AuthToken = Google_AuthToken(),
                ActivityTemplate = activityTemplate
            };
            using (var crateStorage = CrateManager.GetUpdatableStorage(activity))
            {
                crateStorage.Add(PayloadRaw());
            }
            return activity;
        }

        public ActivityDTO Monitor_Form_Responses_v1_Run_EmptyPayload()
        {
            var activityTemplate = Monitor_Form_Responses_v1_ActivityTemplate();

            var activity = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Label = "Monitor Form Responses",
                AuthToken = Google_AuthToken(),
                ActivityTemplate = activityTemplate
            };
            using (var crateStorage = CrateManager.GetUpdatableStorage(activity))
            {
                crateStorage.Add(PayloadEmptyRaw());
            }
            return activity;
        }

        public static ActivityTemplateDTO Get_Google_Sheet_Data_v1_ActivityTemplate()
        {
            return new ActivityTemplateDTO()
            {
                Id = 1,
                Name = "Get_Google_Sheet_Data_TEST",
                Version = "1"
            };
        }
        public static Fr8DataDTO Get_Google_Sheet_Data_v1_InitialConfiguration_Fr8DataDTO()
        {
            var activityTemplate = Get_Google_Sheet_Data_v1_ActivityTemplate();

            var activityDTO = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Label = "Get Google Sheet Data",
                AuthToken = Google_AuthToken(),
                ActivityTemplate = activityTemplate
            };

            return new Fr8DataDTO { ActivityDTO = activityDTO };
        }

        public ActivityDTO Get_Google_Sheet_Data_v1_Followup_Configuration_Request_ActivityDTO_With_Crates()
        {

            var activityTemplate = Get_Google_Sheet_Data_v1_ActivityTemplate();

            var curActivityDto = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Label = "Get Google Sheet Data",
                AuthToken = Google_AuthToken1(),
                ActivityTemplate = activityTemplate
            };

            return curActivityDto;

        }
        public Crate PackCrate_GoogleSpreadsheets()
        {
            Crate crate;

            var curFields = new List<FieldDTO>()
            {
                new FieldDTO() { Key = "Column_Only", Value = @"https://spreadsheets.google.com/feeds/spreadsheets/private/full/1L2TxytQKnYLtHlB3fZ4lb91FKSmmoFk6FJipuDW0gWo" },
                new FieldDTO() { Key = "Row_Only", Value = @"https://spreadsheets.google.com/feeds/spreadsheets/private/full/126yxCJDSZHJoR6d8BYk0wW7tZpl2pcl29F8QXIYVGMQ"},
                new FieldDTO() {Key = "Row_And_Column", Value = @"https://spreadsheets.google.com/feeds/spreadsheets/private/full/1v67fCdV9NItrKRgLHPlp3CS2ia9duUkwKQOAUcQciJ0"},
                new FieldDTO(){Key="Empty_First_Row", Value = @"https://spreadsheets.google.com/feeds/spreadsheets/private/full/1Nzf_s2OyZTxG8ppxzvypH6s1ePvUT_ALPffZchuM14o"}
            }.ToArray();
            crate = CrateManager.CreateDesignTimeFieldsCrate("Select a Google Spreadsheet", curFields);

            return crate;
        }
        public StandardFileDescriptionCM GetUpstreamCrate()
        {
            return new StandardFileDescriptionCM
            {
                DockyardStorageUrl = "https://spreadsheets.google.com/feeds/spreadsheets/private/full/1L2TxytQKnYLtHlB3fZ4lb91FKSmmoFk6FJipuDW0gWo",
                Filename = "Column_Only",
                Filetype = "Google_Spreadsheet"
            };
        }
        private Crate Get_Google_Sheet_Data_v1_PackCrate_ConfigurationControls(Tuple<string, string> spreadsheetTuple)
        {
            var activityUi = new Get_Google_Sheet_Data_v1.ActivityUi();
            activityUi.SpreadsheetList.ListItems = new[] { new ListItem { Key = spreadsheetTuple.Item1, Value = spreadsheetTuple.Item2 } }.ToList();
            activityUi.SpreadsheetList.selectedKey = spreadsheetTuple.Item1;
            activityUi.SpreadsheetList.Value = spreadsheetTuple.Item2;
            return PackControlsCrate(activityUi.Controls.ToArray());
        }

        public void Get_Google_Sheet_Data_v1_AddPayload(ActivityDTO activityDTO, string spreadsheet)
        {
            var caseTuple = CaseTuple(spreadsheet);
            var configurationControlsCrate = Get_Google_Sheet_Data_v1_PackCrate_ConfigurationControls(caseTuple);
            using (var crateStorage = CrateManager.GetUpdatableStorage(activityDTO))
            {
                crateStorage.Add(configurationControlsCrate);
            }
        }

        public Tuple<string, string> CaseTuple(string spreadsheet)
        {
            switch (spreadsheet)
            {
                case "Row_And_Column":
                    return new Tuple<string, string>("Row_And_Column", "https://spreadsheets.google.com/feeds/spreadsheets/private/full/1v67fCdV9NItrKRgLHPlp3CS2ia9duUkwKQOAUcQciJ0");
                case "Row_Only":
                    return new Tuple<string, string>("Row_Only", "https://spreadsheets.google.com/feeds/spreadsheets/private/full/126yxCJDSZHJoR6d8BYk0wW7tZpl2pcl29F8QXIYVGMQ");
                case "Column_Only":
                    return new Tuple<string, string>("Column_Only", "https://spreadsheets.google.com/feeds/spreadsheets/private/full/1L2TxytQKnYLtHlB3fZ4lb91FKSmmoFk6FJipuDW0gWo");
                case "Empty_First_Row":
                    return new Tuple<string, string>("Empty_First_Row", "https://spreadsheets.google.com/feeds/spreadsheets/private/full/1Nzf_s2OyZTxG8ppxzvypH6s1ePvUT_ALPffZchuM14o");
                default:
                    return null;
            }
        }

    }
}
