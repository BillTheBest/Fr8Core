using Data.Entities;
using TerminalBase.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Hub.Managers;
using Newtonsoft.Json;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Infrastructure;
using terminalDocuSign.Services;
using Data.States;
using Data.Validations;

namespace terminalDocuSign.Actions
{
    public class Monitor_DocuSign_Envelope_Activity_v1 : BaseDocuSignActivity
    {

        private const string DocuSignConnectName = "fr8DocuSignConnectConfiguration";

        private const string DocuSignOnEnvelopeSentEvent = "Sent";

        private const string DocuSignOnEnvelopeReceivedEvent = "Delivered";

        private const string DocuSignOnEnvelopeSignedEvent = "Completed";

        private const string RecipientSignedEventName = "RecipientSigned";

        private const string RecipientCompletedEventName = "RecipientCompleted";

        private const string EnvelopeSentEventname = "EnvelopeSent";

        private const string EnvelopeRecievedEventName = "EnvelopeReceived";


        public override async Task<ActivityDO> Configure(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            CheckAuthentication(authTokenDO);
            return await ProcessConfigurationRequest(curActivityDO, ConfigurationEvaluator, authTokenDO);
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            return CrateManager.IsStorageEmpty(curActivityDO)
                ? ConfigurationRequestType.Initial
                : ConfigurationRequestType.Followup;
        }

        private void GetTemplateRecipientPickerValue(ActivityDO curActivityDO, out string selectedOption, out string selectedValue, out string selectedTemplate)
        {
            ActivityUi activityUi = CrateManager.GetStorage(curActivityDO).FirstCrate<StandardConfigurationControlsCM>(x => x.Label == "Configuration_Controls").Content;
            selectedOption = string.Empty;
            selectedValue = string.Empty;
            selectedTemplate = string.Empty;
            if (activityUi.BasedOnTemplateOption.Selected)
            {
                selectedOption = activityUi.BasedOnTemplateOption.Name;
                selectedTemplate = activityUi.TemplateList.selectedKey;
                selectedValue = activityUi.TemplateList.Value;
            }
            else if (activityUi.SentToRecipientOption.Selected)
            {
                selectedOption = activityUi.SentToRecipientOption.Name;
                selectedValue = activityUi.Recipient.Value;
                        }
                    }                   

        private DocuSignEvents GetUserSelectedEnvelopeEvents(ActivityDO curActivityDO)
        {
            ActivityUi activityUi = GetConfigurationControls(curActivityDO);
            return new DocuSignEvents
        {
                EnvelopeSent = activityUi?.EnvelopeSentOption?.Selected ?? false,
                EnvelopRecieved = activityUi?.EnvelopeRecievedOption?.Selected ?? false,
                EnvelopeSigned = activityUi?.EnvelopeSignedOption?.Selected ?? false
            };
        }

        public override Task<ActivityDO> Activate(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            return Task.FromResult(curActivityDO);
        }

        protected internal override ValidationResult ValidateActivityInternal(ActivityDO curActivityDO)
        {
            var errorMessages = new List<string>();
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                ActivityUi activityUi = GetConfigurationControls(crateStorage);
                if (activityUi == null)
                {
                    return new ValidationResult(DocuSignValidationUtils.ControlsAreNotConfiguredErrorMessage);
                }
                errorMessages.Add(activityUi.EnvelopeSignedOption.ErrorMessage
                                  = AtLeastOneNotificationIsSelected(activityUi)
                                        ? string.Empty
                                        : "At least one notification option must be selected");

                errorMessages.Add(activityUi.TemplateRecipientOptionSelector.ErrorMessage
                                  = EnvelopeConditionIsSelected(activityUi)
                                        ? string.Empty
                                        : "At least one envelope option must be selected");

                errorMessages.Add(activityUi.Recipient.ErrorMessage
                    = RecipientIsRequired(activityUi)
                        ? DocuSignValidationUtils.ValueIsSet(activityUi.Recipient)
                            ? activityUi.Recipient.Value.IsValidEmailAddress()
                                ? string.Empty
                                : DocuSignValidationUtils.RecipientIsNotValidErrorMessage
                            : DocuSignValidationUtils.RecipientIsNotSpecifiedErrorMessage
                        : string.Empty);

                errorMessages.Add(activityUi.TemplateList.ErrorMessage
                                  = TemplateIsRequired(activityUi)
                                        ? DocuSignValidationUtils.AtLeastOneItemExists(activityUi.TemplateList)
                                              ? DocuSignValidationUtils.ItemIsSelected(activityUi.TemplateList)
                                                    ? string.Empty
                                                    : DocuSignValidationUtils.TemplateIsNotSelectedErrorMessage
                                              : DocuSignValidationUtils.NoTemplateExistsErrorMessage
                                        : string.Empty);
            }
            errorMessages.RemoveAll(string.IsNullOrEmpty);
            return errorMessages.Count == 0 ? ValidationResult.Success : new ValidationResult(string.Join(Environment.NewLine, errorMessages));
        }

        protected override string ActivityUserFriendlyName => "Monitor DocuSign Envelope Activity";

        private bool TemplateIsRequired(ActivityUi activityUi)
        {
            return activityUi.BasedOnTemplateOption.Selected;
        }
        private bool RecipientIsRequired(ActivityUi activityUi)
        {
            return activityUi.SentToRecipientOption.Selected;
        }
        private bool AtLeastOneNotificationIsSelected(ActivityUi activityUi)
        {
            return activityUi.EnvelopeRecievedOption.Selected
                   || activityUi.EnvelopeSentOption.Selected
                   || activityUi.EnvelopeSignedOption.Selected;
        }
        private bool EnvelopeConditionIsSelected(ActivityUi activityUi)
        {
            return activityUi.SentToRecipientOption.Selected || activityUi.BasedOnTemplateOption.Selected;
        }
        /// <summary>
        /// Creates or Updates a Docusign connect configuration named "DocuSignConnectName" for current user
        /// </summary>
        private void CreateOrUpdateDocuSignConnectConfiguration(DocuSignEvents events)
        {
            //prepare envelope events based on the input parameters
            var envelopeEvents = new List<string>(3);
            if (events.EnvelopeSent)
            {
                envelopeEvents.Add(DocuSignOnEnvelopeSentEvent);
            }
            if (events.EnvelopRecieved)
            {
                envelopeEvents.Add(DocuSignOnEnvelopeReceivedEvent);
                }
            if (events.EnvelopeSigned)
            {
                envelopeEvents.Add(DocuSignOnEnvelopeSignedEvent);
                }

        }

        public override Task<ActivityDO> Deactivate(ActivityDO curActivityDO)
        {
            return Task.FromResult(curActivityDO);
        }

        protected internal override async Task<PayloadDTO> RunInternal(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(curActivityDO, containerId);
            //get currently selected option and its value
            string curSelectedOption, curSelectedValue, curSelectedTemplate;
            GetTemplateRecipientPickerValue(curActivityDO, out curSelectedOption, out curSelectedValue, out curSelectedTemplate);
            var envelopeId = string.Empty;
            //retrieve envelope ID based on the selected option and its value
            if (!string.IsNullOrEmpty(curSelectedOption))
            {
                switch (curSelectedOption)
                {
                    case "template":
                        //filter the incoming envelope by template value selected by the user                       
                        var incommingTemplate = GetValueForEventKey(payloadCrates, "TemplateName");
                        if (incommingTemplate != null)
                        {
                            if (curSelectedTemplate == incommingTemplate)
                            {
                                envelopeId = GetValueForEventKey(payloadCrates, "EnvelopeId");
                            }
                            else
                            {
                                //this event isn't about us let's stop execution
                                return TerminateHubExecution(payloadCrates);
                            }
                        }
                        break;
                    case "recipient":
                        //filter incoming envelope by recipient email address specified by the user
                        var curRecipientEmail = GetValueForEventKey(payloadCrates, "RecipientEmail");
                        if (curRecipientEmail != null)
                        {
                            //if the incoming envelope's recipient is user specified one, get the envelope ID
                            if (curRecipientEmail.Equals(curSelectedValue))
                            {
                                envelopeId = GetValueForEventKey(payloadCrates, "EnvelopeId");
                            }
                            else
                            {
                                //this event isn't about us let's stop execution
                                return TerminateHubExecution(payloadCrates);
                            }
                        }
                        break;
                }
            }

            // Make sure that it exists
            if (string.IsNullOrEmpty(envelopeId))
            {
                await Activate(curActivityDO, authTokenDO);
                return TerminateHubExecution(payloadCrates, "Plan successfully activated. It will wait and respond to specified DocuSign Event messages");
            }

            //Create run-time fields
            var fields = CreateDocuSignEventFields();
            foreach (var field in fields)
            {
                field.Value = GetValueForEventKey(payloadCrates, field.Key);
            }

            //Create log message
            var logMessages = new StandardLoggingCM
            {
                Item = new List<LogItemDTO>
                {
                    new LogItemDTO
                    {
                        Data = "Monitor DocuSign activity successfully recieved an envelope ID " + envelopeId,
                        IsLogged = false
                    }
                }
            };

            using (var crateStorage = CrateManager.GetUpdatableStorage(payloadCrates))
            {
                crateStorage.Add(Data.Crates.Crate.FromContent("DocuSign Envelope Payload Data", new StandardPayloadDataCM(fields)));
                crateStorage.Add(Data.Crates.Crate.FromContent("Log Messages", logMessages));
                if (curSelectedOption == "template")
                {
                    var userDefinedFieldsPayload = CreateActivityPayload(curActivityDO, authTokenDO, envelopeId);
                    crateStorage.Add(Data.Crates.Crate.FromContent("DocuSign Envelope Data", userDefinedFieldsPayload));
                }
            }

            return Success(payloadCrates);
        }

        protected override async Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            
            var controlsCrate = PackControls(CreateActivityUi());
            FillDocuSignTemplateSource(controlsCrate, "UpstreamCrate", authTokenDO);
            var eventFields = CrateManager.CreateDesignTimeFieldsCrate("DocuSign Event Fields", AvailabilityType.RunTime, CreateDocuSignEventFields().ToArray());

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Add(controlsCrate);
                crateStorage.Add(eventFields);

                // Remove previously added crate of "Standard Event Subscriptions" schema
                crateStorage.Remove<EventSubscriptionCM>();
                crateStorage.Add(PackEventSubscriptionsCrate(controlsCrate.Get<StandardConfigurationControlsCM>()));
            }
            return await Task.FromResult(curActivityDO);
        }

        protected override Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            //just update the user selected envelope events in the follow up configuration
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                UpdateSelectedEvents(crateStorage);
                string selectedOption, selectedValue, selectedTemplate;
                GetTemplateRecipientPickerValue(curActivityDO, out selectedOption, out selectedValue, out selectedTemplate);
                if (selectedOption == "template")
                    AddOrUpdateUserDefinedFields(curActivityDO, authTokenDO, crateStorage, selectedValue);
            }
            return Task.FromResult(curActivityDO);
        }

        /// <summary>
        /// Updates event subscriptions list by user checked check boxes.
        /// </summary>
        /// <remarks>The configuration controls include check boxes used to get the selected DocuSign event subscriptions</remarks>
        private void UpdateSelectedEvents(ICrateStorage storage)
        {
            ActivityUi activityUi = storage.CrateContentsOfType<StandardConfigurationControlsCM>().First();

            //get selected check boxes (i.e. user wanted to subscribe these DocuSign events to monitor for)
            var curSelectedDocuSignEvents = new List<string>
                                            {
                                                activityUi.EnvelopeSentOption.Selected ? activityUi.EnvelopeSentOption.Name : string.Empty,
                                                activityUi.EnvelopeRecievedOption.Selected ? activityUi.EnvelopeRecievedOption.Name : string.Empty,
                                                activityUi.EnvelopeSignedOption.Selected ? activityUi.EnvelopeSignedOption.Name : string.Empty
                                            };
            if (curSelectedDocuSignEvents.Contains(RecipientSignedEventName))
            {
                if (!curSelectedDocuSignEvents.Contains(RecipientCompletedEventName))
                {
                    curSelectedDocuSignEvents.Add(RecipientCompletedEventName);
                }
            }
            else
            {
                curSelectedDocuSignEvents.Remove(RecipientCompletedEventName);
            }

            //create standard event subscription crate with user selected DocuSign events
            var curEventSubscriptionCrate = CrateManager.CreateStandardEventSubscriptionsCrate("Standard Event Subscriptions", "DocuSign",
                curSelectedDocuSignEvents.Where(x => !string.IsNullOrEmpty(x)).ToArray());

            storage.Remove<EventSubscriptionCM>();
            storage.Add(curEventSubscriptionCrate);
        }

        private Crate PackEventSubscriptionsCrate(StandardConfigurationControlsCM configurationFields)
        {
            var subscriptions = new List<string>();
            ActivityUi activityUi = configurationFields;
            if (activityUi.EnvelopeSentOption.Selected)
                    {
                subscriptions.Add(EnvelopeSentEventname);
                    }
            if (activityUi.EnvelopeRecievedOption.Selected)
            {
                subscriptions.Add(EnvelopeRecievedEventName);
                }
            if (activityUi.EnvelopeSignedOption.Selected)
            {
                subscriptions.Add(RecipientSignedEventName);
                subscriptions.Add(RecipientCompletedEventName);
            }
            return CrateManager.CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                "DocuSign",
                subscriptions.ToArray());
        }

        private ActivityUi CreateActivityUi()
        {
            var result = new ActivityUi
        {
                ActivityDescription = new TextArea
            {
                IsReadOnly = true,
                Label = "",
                Value = "<p>Process incoming DocuSign Envelope notifications if the following are true:</p>"
                },
                EnvelopeSentOption = new CheckBox
            {
                Label = "You sent a DocuSign Envelope",
                    Name = EnvelopeSentEventname,
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig },
                },
                EnvelopeRecievedOption = new CheckBox
                {
                    Label = "Someone received an Envelope you sent",
                    Name = EnvelopeRecievedEventName,
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                },
                EnvelopeSignedOption = new CheckBox
                {
                    Label = "One of your Recipients signed an Envelope",
                    Name = RecipientSignedEventName,
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                },
                Recipient = new TextBox
                {
                    Label = "",
                    Name = "RecipientValue",
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                },
                SentToRecipientOption = new RadioButtonOption
            {
                    Selected = false,
                    Name = "recipient",
                    Value = "Was sent to a specific recipient"
                },
                TemplateList = new DropDownList
                {
                    Label = "",
                    Name = "UpstreamCrate",
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig },
                    ShowDocumentation = ActivityResponseDTO.CreateDocumentationResponse("Minicon", "ExplainMonitoring")
                },
                BasedOnTemplateOption = new RadioButtonOption
            {
                    Selected = false,
                    Name = "template",
                    Value = "Was based on a specific template"
                },
                TemplateRecipientOptionSelector = new RadioButtonGroup
                {
                    Label = "The envelope:",
                    GroupName = "TemplateRecipientPicker",
                    Name = "TemplateRecipientPicker",
                    Events = new List<ControlEvent> { new ControlEvent("onChange", "requestConfig") }
                }
            };
            result.BasedOnTemplateOption.Controls = new List<ControlDefinitionDTO> { result.TemplateList };
            result.SentToRecipientOption.Controls = new List<ControlDefinitionDTO> { result.Recipient };
            result.TemplateRecipientOptionSelector.Radios = new List<RadioButtonOption> { result.SentToRecipientOption, result.BasedOnTemplateOption };
            return result;
        }

        private struct DocuSignEvents
        {
            public bool EnvelopeSent { get; set; }
            public bool EnvelopRecieved { get; set; }
            public bool EnvelopeSigned { get; set; }
        }

        private class ActivityUi
        {
            public TextArea ActivityDescription { get; set; }
            public CheckBox EnvelopeSentOption { get; set; }
            public CheckBox EnvelopeRecievedOption { get; set; }
            public CheckBox EnvelopeSignedOption { get; set; }
            public RadioButtonGroup TemplateRecipientOptionSelector { get; set; }
            public RadioButtonOption BasedOnTemplateOption { get; set; }
            public DropDownList TemplateList { get; set; }
            public RadioButtonOption SentToRecipientOption { get; set; }
            public TextBox Recipient { get; set; }

            public static implicit operator ActivityUi(StandardConfigurationControlsCM controlsManifest)
            {
                if (controlsManifest == null)
                {
                    return null;
                }
                try
                    {
                    var result = new ActivityUi
                        {
                                     ActivityDescription = (TextArea)controlsManifest.Controls[0],
                                     EnvelopeSentOption = (CheckBox)controlsManifest.Controls[1],
                                     EnvelopeRecievedOption = (CheckBox)controlsManifest.Controls[2],
                                     EnvelopeSignedOption = (CheckBox)controlsManifest.Controls[3],
                                     TemplateRecipientOptionSelector = (RadioButtonGroup)controlsManifest.Controls[4]
                                 };
                    result.SentToRecipientOption = result.TemplateRecipientOptionSelector.Radios[0];
                    result.Recipient = (TextBox)result.SentToRecipientOption.Controls[0];
                    result.BasedOnTemplateOption = result.TemplateRecipientOptionSelector.Radios[1];
                    result.TemplateList = (DropDownList)result.BasedOnTemplateOption.Controls[0];
                    return result;
                }
                catch
                            {
                    return null;
                            }
                        }

            public static implicit operator StandardConfigurationControlsCM(ActivityUi activityUi)
                        {
                if (activityUi == null)
                            {
                    return null;
                    }
                return new StandardConfigurationControlsCM(activityUi.ActivityDescription,
                                                           activityUi.EnvelopeSentOption,
                                                           activityUi.EnvelopeRecievedOption,
                                                           activityUi.EnvelopeSignedOption,
                                                           activityUi.TemplateRecipientOptionSelector);
                }
        }
    }
}
