﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using StructureMap;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;

using Hub.Managers;
using TerminalBase.Infrastructure;
using TerminalBase.BaseClasses;
using Utilities;
using terminalSendGrid.Infrastructure;

namespace terminalSendGrid.Actions
{
    public class SendEmailViaSendGrid_v1 : BaseTerminalAction
    {
        // moved the EmailPackager ObjectFactory here since the basepluginAction will be called by others and the dependency is defiend in pluginsendGrid
        private IConfigRepository _configRepository;
        private IEmailPackager _emailPackager;

        public SendEmailViaSendGrid_v1()
        {
            _configRepository = ObjectFactory.GetInstance<IConfigRepository>();
            _emailPackager = ObjectFactory.GetInstance<IEmailPackager>();
        }

        public override async Task<ActionDO> Configure(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            return await ProcessConfigurationRequest(curActionDO, EvaluateReceivedRequest, authTokenDO);
        }

        /// <summary>
        /// this entire function gets passed as a delegate to the main processing code in the base class
        /// currently many actions have two stages of configuration, and this method determines which stage should be applied
        /// </summary>
        private ConfigurationRequestType EvaluateReceivedRequest(ActionDO curActionDO)
        {
            if (Crate.IsStorageEmpty(curActionDO))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }
        
        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                updater.CrateStorage.Clear();
                updater.CrateStorage.Add(CreateControlsCrate());
                updater.CrateStorage.Add(await GetAvailableDataFields(curActionDO));
            }

            return curActionDO;
        }
        
        protected override async Task<ActionDO> FollowupConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                updater.CrateStorage.RemoveByLabel("Upstream Terminal-Provided Fields");
                updater.CrateStorage.Add(await GetAvailableDataFields(curActionDO));
            }

            return curActionDO;
        }

        /// <summary>
        /// Create EmailAddress RadioButtonGroup
        /// </summary>
        /// <returns></returns>
        private ControlDefinitionDTO CreateEmailAddressTextSourceControl()
        {
            var control = CreateSpecificOrUpstreamValueChooser(
                "For the Email Address",
                "EmailAddress",
                "Upstream Terminal-Provided Fields",
                "EmailAddress"
            );

            return control;
        }

        /// <summary>
        /// Create EmailSubject RadioButtonGroup
        /// </summary>
        /// <returns></returns>
        private ControlDefinitionDTO CreateEmailSubjectTextSourceControl()
        {
            var control = CreateSpecificOrUpstreamValueChooser(
                "For the Email Subject",
                "EmailSubject",
                "Upstream Terminal-Provided Fields"
            );

            return control;
        }

        /// <summary>
        /// Create EmailBody RadioButtonGroup
        /// </summary>
        /// <returns></returns>
        private ControlDefinitionDTO CreateEmailBodyTextSourceControl()
        {
            var control = CreateSpecificOrUpstreamValueChooser(
                "For the Email Body",
                "EmailBody",
                "Upstream Terminal-Provided Fields"
            );

            return control;
        }

        private Crate CreateControlsCrate()
        {
            var controls = new[]
            {
                CreateEmailAddressTextSourceControl(),
                CreateEmailSubjectTextSourceControl(),
                CreateEmailBodyTextSourceControl()
            };

            return Crate.CreateStandardConfigurationControlsCrate("Send Grid", controls);
        }

        private async Task<Crate> GetAvailableDataFields(ActionDO curActionDO)
        {
            var curUpstreamFields =
                (await GetDesignTimeFields(curActionDO, CrateDirection.Upstream))
                    .Fields
                    .ToArray();

            var crateDTO = Crate.CreateDesignTimeFieldsCrate(
                "Upstream Terminal-Provided Fields",
                curUpstreamFields
            );

            return crateDTO;
        }

        public object Activate(ActionDO curActionDO)
        {
            //not currently any requirements that need attention at Activation Time
            return curActionDO;
        }

        public object Deactivate(ActionDO curActionDO)
        {
            return curActionDO;
        }

        private string CreateEmailHTMLText(string emailBody)
        {
            var template = @"<html><body>{0}</body></html>";
            var htmlText = string.Format(template, emailBody);

            return htmlText;
        }

        public async Task<PayloadDTO> Run(ActionDO curActionDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var fromAddress = _configRepository.Get("OutboundFromAddress");

            var processPayload = await GetProcessPayload(curActionDO, containerId);

            var emailAddress = ExtractSpecificOrUpstreamValue(
                Crate.GetStorage(curActionDO.CrateStorage),
                Crate.FromDto(processPayload.CrateStorage),
                "EmailAddress"
            );
            var emailSubject = ExtractSpecificOrUpstreamValue(
                Crate.GetStorage(curActionDO.CrateStorage),
                Crate.FromDto(processPayload.CrateStorage),
                "EmailSubject"
            );
            var emailBody = ExtractSpecificOrUpstreamValue(
                Crate.GetStorage(curActionDO.CrateStorage),
                Crate.FromDto(processPayload.CrateStorage),
                "EmailBody"
            );

            var mailerDO = new TerminalMailerDO()
            {
                Email = new EmailDO()
                {
                    From = new EmailAddressDO
                    {
                        Address = fromAddress,
                        Name = "Fr8 Operations"
                    },

                    Recipients = new List<RecipientDO>()
                    {
                        new RecipientDO()
                        {
                            EmailAddress = new EmailAddressDO(emailAddress),
                            EmailParticipantType = EmailParticipantType.To
                        }
                    },
                    Subject = emailSubject,
                    HTMLText = CreateEmailHTMLText(emailBody)
                }
            };

            await _emailPackager.Send(mailerDO);

            return processPayload;
        }
    }
}