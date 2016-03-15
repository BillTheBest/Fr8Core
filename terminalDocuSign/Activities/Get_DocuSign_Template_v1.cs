﻿
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Data.Control;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers;
using Newtonsoft.Json;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Services;
using TerminalBase.Infrastructure;
using Data.Entities;
using Data.Crates;
using Data.States;
using Utilities;
using terminalDocuSign.Infrastructure;
using terminalDocuSign.Services.New_Api;
using Data.Constants;

namespace terminalDocuSign.Actions
{
    public class Get_DocuSign_Template_v1 : BaseDocuSignActivity
    {


        public override async Task<ActivityDO> Configure(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            if (NeedsAuthentication(authTokenDO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            return await ProcessConfigurationRequest(curActivityDO, ConfigurationEvaluator, authTokenDO);
        }

        protected override string ActivityUserFriendlyName => "Get DocuSign Template";

        protected internal override async Task<PayloadDTO> RunInternal(ActivityDO activityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(activityDO, containerId);
            //Get template Id
            var control = (DropDownList)FindControl(CrateManager.GetStorage(activityDO), "Available_Templates");
            string selectedDocusignTemplateId = control.Value;
            if (selectedDocusignTemplateId == null)
            {
                return Error(payloadCrates, "No Template was selected at design time", ActivityErrorCode.DESIGN_TIME_DATA_MISSING);
            }

            var config = DocuSignService.SetUp(authTokenDO);
            //lets download specified template from user's docusign account
            var downloadedTemplate = DocuSignService.DownloadDocuSignTemplate(config, selectedDocusignTemplateId);
            //and add it to payload
            var templateCrate = CreateDocuSignTemplateCrateFromDto(downloadedTemplate);
            using (var crateStorage = CrateManager.GetUpdatableStorage(payloadCrates))
            {
                crateStorage.Add(templateCrate);
            }
            return Success(payloadCrates);
        }

        private Crate CreateDocuSignTemplateCrateFromDto(DocuSignTemplateDTO template)
        {
            var manifest = new DocuSignTemplateCM
            {
                Body = JsonConvert.SerializeObject(template),
                CreateDate = DateTime.UtcNow,
                Name = template.Name,
                Status = template.EnvelopeData.status
            };

            return Data.Crates.Crate.FromContent("DocuSign Template", manifest);
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
            var configurationCrate = CreateControlsCrate();
            FillDocuSignTemplateSource(configurationCrate, "Available_Templates", authTokenDO);

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Clear();
                crateStorage.Add(configurationCrate);
            }
            return await Task.FromResult(curActivityDO);
        }

        protected internal override ValidationResult ValidateActivityInternal(ActivityDO curActivityDO)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                var configControls = GetConfigurationControls(crateStorage);
                if (configControls == null)
                {
                    return new ValidationResult(DocuSignValidationUtils.ControlsAreNotConfiguredErrorMessage);
                }
                var templateList = configControls.Controls.OfType<DropDownList>().First();
                templateList.ErrorMessage =
                    DocuSignValidationUtils.AtLeastOneItemExists(templateList)
                        ? DocuSignValidationUtils.ItemIsSelected(templateList)
                              ? string.Empty
                              : DocuSignValidationUtils.TemplateIsNotSelectedErrorMessage
                        : DocuSignValidationUtils.NoTemplateExistsErrorMessage;
                return string.IsNullOrEmpty(templateList.ErrorMessage) ? ValidationResult.Success : new ValidationResult(templateList.ErrorMessage);
            }
        }

        private Crate CreateControlsCrate()
        {
            var availableTemplates = new DropDownList
            {
                Label = "Get which template",
                Name = "Available_Templates",
                Value = null,
                Source = null,
                Events = new List<ControlEvent> { ControlEvent.RequestConfig },
            };
            return PackControlsCrate(availableTemplates);
        }
    }
}