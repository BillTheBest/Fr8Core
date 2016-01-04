﻿using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Control;
using Hub.Managers;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using terminalSalesforce.Infrastructure;
using terminalSalesforce.Services;

namespace terminalSalesforce.Actions
{
    public class Create_Account_v1 : BaseTerminalAction
    {
        ISalesforceIntegration _salesforce = new SalesforceIntegration();

        public override async Task<ActionDO> Configure(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            CheckAuthentication(authTokenDO);

            return await ProcessConfigurationRequest(curActionDO, ConfigurationEvaluator, authTokenDO);
        }

        public async Task<PayloadDTO> Run(ActionDO curActionDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {

            var payloadCrates = await GetPayload(curActionDO, containerId);

            if (NeedsAuthentication(authTokenDO))
            {
                return NeedsAuthenticationError(payloadCrates);
            }

            var accountName = ExtractControlFieldValue(curActionDO, "accountName");
            if (string.IsNullOrEmpty(accountName))
            {
                return Error(payloadCrates, "No account name found in action.");
            }

            bool result = _salesforce.CreateAccount(curActionDO, authTokenDO);

            return Success(payloadCrates);
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            return ConfigurationRequestType.Initial;
        }

        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            var accountName = new TextBox()
            {
                Label = "Account Name",
                Name = "accountName"

            };
            var accountNumber = new TextBox()
            {
                Label = "Account Number",
                Name = "accountNumber",
                Required = true
            };

            var phone = new TextBox()
            {
                Label = "Phone",
                Name = "phone",
                Required = true
            };

            var controls = PackControlsCrate(accountName, accountNumber, phone);

            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                updater.CrateStorage.Clear();
                updater.CrateStorage.Add(controls);
            }

            return await Task.FromResult(curActionDO);
        }
    }
}