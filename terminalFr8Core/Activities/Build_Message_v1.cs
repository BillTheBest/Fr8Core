﻿using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Hub.Managers;
using Data.Interfaces.Manifests;
using Data.Control;
using Data.States;
using System.Text.RegularExpressions;

namespace terminalFr8Core.Actions
{
    public class Build_Message_v1 : BaseTerminalActivity
    {
        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            if (CrateManager.IsStorageEmpty(curActivityDO))
            {
                return ConfigurationRequestType.Initial;
            }
            else
            {
                return ConfigurationRequestType.Followup;
            }
        }

        protected async override Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            var configurationCrate = CreateControlsCrate();
            await FillAvailableFieldsSource(configurationCrate, "AvailableFields", curActivityDO);

            using (var updater = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                updater.Clear();
                updater.Add(configurationCrate);
            }
            AddNameDesignTimeField(curActivityDO);
            return curActivityDO;
        }

        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            AddNameDesignTimeField(curActivityDO);
            return await base.FollowupConfigurationResponse(curActivityDO, authTokenDO);
        }

        private Crate CreateControlsCrate()
        {
            var controls = new List<ControlDefinitionDTO>()
            {
                new TextBox()
                {
                    Label = "Name",
                    Name = "Name",
                    Source = new FieldSourceDTO
                    {
                        Label = "Build Message",
                        ManifestType = CrateManifestTypes.StandardDesignTimeFields
                    },
                    Events = new List<ControlEvent> {new ControlEvent("onChange", "requestConfig")}
                },
                new TextArea()
                {
                    Label = "Body",
                    Name = "Body"
                    ,IsReadOnly = false
                    , Required = true
                },
                new DropDownList
                {
                    Name = "AvailableFields",
                    Required = true,
                    Label = "Available Fields",
                    Source = null
                }
            };

            return CrateManager.CreateStandardConfigurationControlsCrate("Craft a Message", controls.ToArray());
        }

        private void AddNameDesignTimeField(ActivityDO curActivityDO)
        {
            var storage = CrateManager.GetStorage(curActivityDO);
            var buildMsgConfigurationControls = storage.CrateContentsOfType<StandardConfigurationControlsCM>(c => String.Equals(c.Label, "Craft a Message", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            var key = ((TextBox)GetControl(buildMsgConfigurationControls, "Name")).Value;
            using (var updater = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                updater.RemoveByLabel("Build Message");

                var bodyFieldDTO = new List<FieldDTO> { new FieldDTO() { Key = key, Value = key, Availability = AvailabilityType.RunTime } };
                updater.Add(CrateManager.CreateDesignTimeFieldsCrate("Build Message", bodyFieldDTO, AvailabilityType.RunTime));
            }
        }

        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(curActivityDO, containerId);

            var payloadCrateStorage = CrateManager.GetStorage(payloadCrates);
            var payloadDataObjects = payloadCrateStorage.CratesOfType<StandardPayloadDataCM>().ToList();

            if (payloadDataObjects.Count > 0)
            {
                var fieldsDTO = payloadDataObjects.SelectMany(s => s.Content.PayloadObjects).SelectMany(s => s.PayloadObject).ToList();

                if (fieldsDTO.Count > 0)
                {
                    //get build message configuration controls for interpolation
                    var storage = CrateManager.GetStorage(curActivityDO);
                    var buildMsgConfigurationControls = storage.CrateContentsOfType<StandardConfigurationControlsCM>(c => String.Equals(c.Label, "Craft a Message", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                    if (buildMsgConfigurationControls != null)
                    {
                        var bodyMsg = ((TextArea)GetControl(buildMsgConfigurationControls, "Body")).Value;
                        var bodyMsgInterpolated = ((TextArea)GetControl(buildMsgConfigurationControls, "Body")).Value;
                        //search for placeholders ^\[.*?\]$
                        Regex regexPattern = new Regex(@"\[.*?\]");
                        var resultMatch = regexPattern.Matches(bodyMsg);

                        //if found placeholders
                        if (resultMatch.Count > 0)
                        {
                            //match payloadFieldsDTO and get its value
                            foreach (var placeholder in resultMatch.Cast<Match>().Select(match => match.Value).ToList())
                            {
                                var fieldDTO = fieldsDTO.Where(w => w.Key.ToLower() == placeholder.ToLower().TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' })).FirstOrDefault();
                                if (fieldDTO != null)
                                {
                                    //replace placeholder with its value
                                    bodyMsgInterpolated = bodyMsgInterpolated.Replace(placeholder, fieldDTO.Value);
                                }
                            }
                        }

                        //add the body to payloadcrates
                        List<FieldDTO> payloadFieldDTO = new List<FieldDTO>();
                        FieldDTO _fieldDTO = new FieldDTO();
                        _fieldDTO.Key = ((TextBox)GetControl(buildMsgConfigurationControls, "Name")).Value;
                        _fieldDTO.Value = bodyMsgInterpolated;
                        payloadFieldDTO.Add(_fieldDTO);

                        using (var updater = CrateManager.GetUpdatableStorage(payloadCrates))
                        {
                            updater.Add(Data.Crates.Crate.FromContent("BuildAMessage", new StandardPayloadDataCM(payloadFieldDTO)));
                        }
                    }
                }
            }

            return Success(payloadCrates);
        }

        #region Fill Source
        private async Task FillAvailableFieldsSource(Crate configurationCrate, string controlName, ActivityDO curActivityDO)
        {
            var configurationControl = configurationCrate.Get<StandardConfigurationControlsCM>();
            var control = configurationControl.FindByNameNested<DropDownList>(controlName);
            if (control != null)
            {
                control.ListItems = await GetAvailableFields(curActivityDO);
            }
        }

        private async Task<List<ListItem>> GetAvailableFields(ActivityDO curActivityDO)
        {
            var upstreamFieldsAddress = await MergeUpstreamFields<FieldDescriptionsCM>(curActivityDO, "Available Fields");
            if (upstreamFieldsAddress != null)
            {
                FieldDescriptionsCM curFieldDescriptionsCrate = ((Crate<FieldDescriptionsCM>)upstreamFieldsAddress).Content;
                return curFieldDescriptionsCrate.Fields.Select(x => new ListItem() { Key = x.Key, Value = x.Value }).ToList();
            }
            return null;
        }
        #endregion
    }
}