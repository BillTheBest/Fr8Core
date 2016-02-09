﻿using System;
using NUnit.Framework;
using Data.Interfaces.DataTransferObjects;
using HealthMonitor.Utility;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Hub.Managers;
using Data.Interfaces.Manifests;
using terminalDocuSignTests.Fixtures;
using Newtonsoft.Json.Linq;
using Data.Crates;
using Data.Control;
using Data.States;

namespace terminalDocuSignTests.Integration
{
    /// <summary>
    /// Mark test case class with [Explicit] attiribute.
    /// It prevents test case from running when CI is building the solution,
    /// but allows to trigger that class from HealthMonitor.
    /// </summary>
    [Explicit]
    [Category("terminalDocuSignTests.Integration")]
    public class Rich_Document_Notifications_v1_EndToEnd_Tests : BaseHubIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalDocuSign"; }
        }

        ActivityDTO solution;
        CrateStorage crateStorage;


        private void ShouldHaveCorrectCrateStructure(CrateStorage crateStorage)
        {
            Assert.True(crateStorage.CratesOfType<StandardConfigurationControlsCM>().Any(), "Crate StandardConfigurationControlsCM is missing in API response.");
            Assert.True(crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Any(c => c.Label == "AvailableTemplates"), "StandardDesignTimeFieldsCM with label \"AvailableTemplates\" is missing in API response.");
            Assert.True(crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Any(c => c.Label == "AvailableHandlers"), "StandardDesignTimeFieldsCM with label \"AvailableHandlers\" is missing in API response.");
            Assert.True(crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Any(c => c.Label == "AvailableRecipientEvents"), "StandardDesignTimeFieldsCM with label \"AvailableRecipientEvents\" is missing in API response.");

            var templatesCrate = crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Single(c => c.Label == "AvailableTemplates");
            var handlersCrate = crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Single(c => c.Label == "AvailableHandlers");
            var recipientEventsCrate = crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Single(c => c.Label == "AvailableRecipientEvents");

            Assert.True(templatesCrate.Content.Fields.Any(), "There are no fields in AvailableTemplates Crate");
            Assert.True(handlersCrate.Content.Fields.Any(), "There are no fields in AvailableHandlers Crate");
            Assert.True(recipientEventsCrate.Content.Fields.Any(), "There are no fields in AvailableRecipientEvents Crate");
        }

        [Test]
        public async void Rich_Document_Notifications_EndToEnd()
        {
            string baseUrl = GetHubApiBaseUrl();
            var docusignTerminalUrl = GetTerminalUrl();
            var solutionCreateUrl = baseUrl + "actions/create?solutionName=Rich_Document_Notifications";

            //
            // Create solution
            //
            var plan = await HttpPostAsync<string, RouteFullDTO>(solutionCreateUrl, null);
            var solution = plan.Subroutes.FirstOrDefault().Activities.FirstOrDefault();

            //
            // Send configuration request without authentication token
            //
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure?id=" + solution.Id, solution);
            crateStorage = _crate.FromDto(this.solution.CrateStorage);
            var stAuthCrate = crateStorage.CratesOfType<StandardAuthenticationCM>().FirstOrDefault();
            bool defaultDocuSignAuthTokenExists = stAuthCrate == null;

            if (!defaultDocuSignAuthTokenExists)
            {
                //
                // Authenticate with DocuSign
                //
                var creds = new CredentialsDTO()
                {
                    Username = "freight.testing@gmail.com",
                    Password = "I6HmXEbCxN",
                    IsDemoAccount = true,
                    TerminalId = solution.ActivityTemplate.TerminalId
                };
                var token = await HttpPostAsync<CredentialsDTO, JObject>(baseUrl + "authentication/token", creds);
                Assert.AreEqual(false, String.IsNullOrEmpty(token["authTokenId"].Value<string>()), "AuthTokenId is missing in API response.");
                Guid tokenGuid = Guid.Parse(token["authTokenId"].Value<string>());

                //
                // Asociate token with action
                //
                var applyToken = new ManageAuthToken_Apply()
                {
                    ActivityId = solution.Id,
                    AuthTokenId = tokenGuid,
                    IsMain = true
                };
                await HttpPostAsync<ManageAuthToken_Apply[], string>(baseUrl + "ManageAuthToken/apply", new ManageAuthToken_Apply[] { applyToken });
            }

            //
            // Send configuration request with authentication token
            //
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure?id=" + solution.Id, solution);
            crateStorage = _crate.FromDto(this.solution.CrateStorage);

            ShouldHaveCorrectCrateStructure(crateStorage);
            Assert.True(this.solution.ChildrenActions.Length == 0);

            var controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var controls = controlsCrate.Content.Controls;

            #region CHECK_CONFIGURATION_CONTROLS

            Assert.AreEqual(5, controls.Count);
            Assert.True(controls.Any(c => c.Type == ControlTypes.DropDownList && c.Name == "NotificationHandler"));
            Assert.True(controls.Any(c => c.Type == ControlTypes.TextBlock && c.Name == "EventInfo"));
            Assert.True(controls.Any(c => c.Type == ControlTypes.DropDownList && c.Name == "RecipientEvent"));
            Assert.True(controls.Any(c => c.Type == ControlTypes.Duration && c.Name == "TimePeriod"));
            Assert.True(controls.Any(c => c.Type == ControlTypes.RadioButtonGroup && c.Name == "Track_Which_Envelopes"));

            var radioButtonGroup = (RadioButtonGroup)controls.Single(c => c.Type == ControlTypes.RadioButtonGroup && c.Name == "Track_Which_Envelopes");
            Assert.AreEqual(2, radioButtonGroup.Radios.Count);
            Assert.True(radioButtonGroup.Radios.Any(c => c.Name == "SpecificRecipient"));
            Assert.True(radioButtonGroup.Radios.Any(c => c.Name == "SpecificTemplate"));

            var specificRecipientOption = (RadioButtonOption)radioButtonGroup.Radios.Single(c => c.Name == "SpecificRecipient");
            Assert.AreEqual(1, specificRecipientOption.Controls.Count);
            Assert.True(specificRecipientOption.Controls.Any(c => c.Name == "SpecificRecipient" && c.Type == ControlTypes.TextBox));

            var specificTemplateOption = (RadioButtonOption)radioButtonGroup.Radios.Single(c => c.Name == "SpecificTemplate");
            Assert.AreEqual(1, specificTemplateOption.Controls.Count);
            Assert.True(specificTemplateOption.Controls.Any(c => c.Name == "SpecificTemplate" && c.Type == ControlTypes.DropDownList));

            #endregion

            //let's make some selections and go for re-configure
            //RDN shouldn't update it's child activity structure until we select a notification method
            radioButtonGroup.Radios[0].Selected = true;
            radioButtonGroup.Radios[1].Selected = false;
            specificRecipientOption.Controls[0].Value = "test@fr8.co";

            using (var updater = _crate.UpdateStorage(this.solution))
            {
                updater.CrateStorage.Remove<StandardConfigurationControlsCM>();
                updater.CrateStorage.Add(controlsCrate);
            }

            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure?id=" + this.solution.Id, this.solution);
            crateStorage = _crate.FromDto(this.solution.CrateStorage);
            ShouldHaveCorrectCrateStructure(crateStorage);
            Assert.True(this.solution.ChildrenActions.Length == 0);

            //everything seems perfect for now
            //let's force RDN for a followup configuration

            var timePeriod = (Duration) controls.Single(c => c.Type == ControlTypes.Duration && c.Name == "TimePeriod");
            var notificationHandler = (DropDownList)controls.Single(c => c.Type == ControlTypes.DropDownList && c.Name == "NotificationHandler");
            var recipientEvent = (DropDownList)controls.Single(c => c.Type == ControlTypes.DropDownList && c.Name == "RecipientEvent");

            timePeriod.Days = 0;
            timePeriod.Hours = 0;
            timePeriod.Minutes = 0;
            var handlersCrate = crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Single(c => c.Label == "AvailableHandlers");
            notificationHandler.Value = handlersCrate.Content.Fields[0].Value;
            notificationHandler.selectedKey = handlersCrate.Content.Fields[0].Key;
            var recipientEventsCrate = crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Single(c => c.Label == "AvailableRecipientEvents");
            recipientEvent.Value = recipientEventsCrate.Content.Fields[0].Value;
            recipientEvent.selectedKey = recipientEventsCrate.Content.Fields[0].Key;

            using (var updater = _crate.UpdateStorage(this.solution))
            {
                updater.CrateStorage.Remove<StandardConfigurationControlsCM>();
                updater.CrateStorage.Add(controlsCrate);
            }

            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure?id=" + this.solution.Id, this.solution);
            crateStorage = _crate.FromDto(this.solution.CrateStorage);

            //from now on our solution should have followup crate structure
            Assert.True(this.solution.ChildrenActions.Length == 5, "Solution child actions failed to create.");

            Assert.True(this.solution.ChildrenActions.Any(a => a.Name == "Monitor Docusign Envelope Activity" && a.Ordering == 1));
            Assert.True(this.solution.ChildrenActions.Any(a => a.Name == "Set Delay" && a.Ordering == 2));
            Assert.True(this.solution.ChildrenActions.Any(a => a.Name == "Query Fr8 Warehouse" && a.Ordering == 3));
            Assert.True(this.solution.ChildrenActions.Any(a => a.Name == "Test Incoming Data" && a.Ordering == 4));
            Assert.True(this.solution.ChildrenActions.Any(a => a.Name == notificationHandler.selectedKey && a.Ordering == 5));

            //
            //Rename route
            //
            var newName = plan.Name + " | " + DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
            await HttpPostAsync<object, RouteFullDTO>(baseUrl + "routes?id=" + plan.Id, new { id = plan.Id, name = newName });

            //let's activate our route
            await HttpPostAsync<string, string>(baseUrl + "routes/activate?planId=" + plan.Id, null);
            
            //everything seems perfect -> let's fake a docusign event
            var fakeDocuSignEventContent = @"<?xml version=""1.0"" encoding=""utf-8""?><DocuSignEnvelopeInformation xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://www.docusign.net/API/3.0""><EnvelopeStatus><RecipientStatuses><RecipientStatus><Type>Signer</Type><Email>test@fr8.co</Email><UserName>Fr8 Test User</UserName><RoutingOrder>1</RoutingOrder><Sent>2016-02-09T04:19:58.41</Sent><DeclineReason xsi:nil=""true"" /><Status>Sent</Status><RecipientIPAddress /><CustomFields /><TabStatuses><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>189</XPosition><YPosition>326</YPosition><TabLabel>Text 5</TabLabel><TabName>Text</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Text</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>675</XPosition><YPosition>504</YPosition><TabLabel>Text 8</TabLabel><TabName>Text</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Text</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>941</XPosition><YPosition>860</YPosition><TabLabel>Checkbox 1</TabLabel><TabName>Checkbox</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Checkbox</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>1022</XPosition><YPosition>860</YPosition><TabLabel>Checkbox 2</TabLabel><TabName>Checkbox</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Checkbox</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>941</XPosition><YPosition>889</YPosition><TabLabel>Checkbox 3</TabLabel><TabName>Checkbox</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Checkbox</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>1022</XPosition><YPosition>889</YPosition><TabLabel>Checkbox 4</TabLabel><TabName>Checkbox</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Checkbox</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>939</XPosition><YPosition>918</YPosition><TabLabel>Checkbox 5</TabLabel><TabName>Checkbox</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Checkbox</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>1022</XPosition><YPosition>918</YPosition><TabLabel>Checkbox 6</TabLabel><TabName>Checkbox</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Checkbox</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>812</XPosition><YPosition>192</YPosition><TabLabel>DateOfBirth</TabLabel><TabName>Text</TabName><TabValue /><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue /><ValidationPattern /><CustomTabType>Date</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>364</XPosition><YPosition>400</YPosition><TabLabel>Condition</TabLabel><TabName>Text</TabName><TabValue>Marthambles</TabValue><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue>Marthambles</OriginalValue><ValidationPattern /><CustomTabType>Text</CustomTabType></TabStatus><TabStatus><TabType>Custom</TabType><Status>Active</Status><XPosition>181</XPosition><YPosition>239</YPosition><TabLabel>Doctor</TabLabel><TabName>Text</TabName><TabValue>Dohemann</TabValue><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue>Dohemann</OriginalValue><ValidationPattern /><CustomTabType>Text</CustomTabType></TabStatus><TabStatus><TabType>FullName</TabType><Status>Active</Status><XPosition>243</XPosition><YPosition>196</YPosition><TabLabel>Name 1</TabLabel><TabName>Name</TabName><TabValue>Bahadir Bozdag</TabValue><DocumentID>1</DocumentID><PageNumber>1</PageNumber><OriginalValue>Joanna Smith</OriginalValue></TabStatus></TabStatuses><AccountStatus>Active</AccountStatus><RecipientId>3c498fd2-499c-414c-a980-6b3a8a108643</RecipientId></RecipientStatus></RecipientStatuses><TimeGenerated>2016-02-09T04:22:25.6749113</TimeGenerated><EnvelopeID>fffb6908-4c84-4a05-9fb4-e3e94d5aaa1a</EnvelopeID><Subject>Please DocuSign: medical_intake_form.pdf</Subject><UserName>Dockyard Developer</UserName><Email>docusign_developer@dockyard.company</Email><Status>Sent</Status><Created>2016-02-09T04:19:40.08</Created><Sent>2016-02-09T04:19:58.567</Sent><ACStatus>Original</ACStatus><ACStatusDate>2016-02-09T04:19:40.08</ACStatusDate><ACHolder>Dockyard Developer</ACHolder><ACHolderEmail>docusign_developer@dockyard.company</ACHolderEmail><ACHolderLocation>DocuSign</ACHolderLocation><SigningLocation>Online</SigningLocation><SenderIPAddress>178.233.137.179</SenderIPAddress><EnvelopePDFHash /><CustomFields /><AutoNavigation>true</AutoNavigation><EnvelopeIdStamping>true</EnvelopeIdStamping><AuthoritativeCopy>false</AuthoritativeCopy><DocumentStatuses><DocumentStatus><ID>1</ID><Name>medical_intake_form.pdf</Name><TemplateName>Medical_Form_v1</TemplateName><Sequence>1</Sequence></DocumentStatus></DocumentStatuses></EnvelopeStatus></DocuSignEnvelopeInformation>";
            var httpContent = new StringContent(fakeDocuSignEventContent, Encoding.UTF8, "application/xml");
            await HttpPostAsync<string>(docusignTerminalUrl + "/terminals/terminalDocuSign/events", httpContent);

            //let's wait 30 seconds before continuing
            await Task.Delay(TimeSpan.FromSeconds(30));
            //we should have received an email about this operation

            //
            // Configure solution
            //
            using (var updater = _crate.UpdateStorage(this.solution))
            {

                updater.CrateStorage.Remove<StandardConfigurationControlsCM>();
                updater.CrateStorage.Add(controlsCrate);
            }
            this.solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure?id=" + this.solution.Id, this.solution);
            crateStorage = _crate.FromDto(this.solution.CrateStorage);
            Assert.AreEqual(2, this.solution.ChildrenActions.Count(), "Solution child actions failed to create.");

            // Delete Google action 
            await HttpDeleteAsync(baseUrl + "actions?id=" + this.solution.ChildrenActions[0].Id);

            // Add Add Payload Manually action
            var activityCategoryParam = new ActivityCategory[] { ActivityCategory.Processors };
            var activityTemplates = await HttpPostAsync<ActivityCategory[], List<WebServiceActionSetDTO>>(baseUrl + "webservices/actions", activityCategoryParam);
            var apmActivityTemplate = activityTemplates.SelectMany(a => a.Actions).Single(a => a.Name == "AddPayloadManually");

            var apmAction = new ActivityDTO()
            {
                ActivityTemplate = apmActivityTemplate,
                ActivityTemplateId = apmActivityTemplate.Id,
                Label = apmActivityTemplate.Label,
                Name = apmActivityTemplate.Name,
                ParentRouteNodeId = this.solution.Id,
                RootRouteNodeId = plan.Id,
                IsTempId = true
            };
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/save", apmAction);
            Assert.NotNull(apmAction, "Add Payload Manually action failed to create");
            Assert.IsTrue(apmAction.Id != default(Guid), "Add Payload Manually action failed to create");

            //
            // Configure Add Payload Manually action
            //

            //Add rows to Add Payload Manually action
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure", apmAction);
            crateStorage = _crate.FromDto(apmAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var fieldList = controlsCrate.Content.Controls.OfType<FieldList>().First();
            fieldList.Value = @"[{""Key"":""Doctor"",""Value"":""Doctor1""},{""Key"":""Condition"",""Value"":""Condition1""}]";

            using (var updater = _crate.UpdateStorage(apmAction))
            {
                updater.CrateStorage.Remove<StandardConfigurationControlsCM>();
                updater.CrateStorage.Add(controlsCrate);
            }

            // Move Add Payload Manually action to the beginning of the plan
            apmAction.Ordering = 1;
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/save", apmAction);
            apmAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure", apmAction);

            //
            // Configure Send DocuSign Envelope action
            //
            var sendEnvelopeAction = this.solution.ChildrenActions.Single(a => a.Name == "Send DocuSign Envelope");

            crateStorage = _crate.FromDto(sendEnvelopeAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var docuSignTemplate = controlsCrate.Content.Controls.OfType<DropDownList>().First();
            docuSignTemplate.Value = "9a4d2154-5b18-4316-9824-09432e62f458";
            docuSignTemplate.selectedKey = "Medical_Form_v1";
            docuSignTemplate.ListItems.Add(new ListItem() { Value = "9a4d2154-5b18-4316-9824-09432e62f458", Key = "Medical_Form_v1" });

            var emailField = controlsCrate.Content.Controls.OfType<TextSource>().First();
            emailField.ValueSource = "specific";
            emailField.Value = "freight.testing@gmail.com";
            emailField.TextValue = "freight.testing@gmail.com";

            using (var updater = _crate.UpdateStorage(sendEnvelopeAction))
            {
                updater.CrateStorage.Remove<StandardConfigurationControlsCM>();
                updater.CrateStorage.Add(controlsCrate);
            }
            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/save", sendEnvelopeAction);
            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure", sendEnvelopeAction);


            //
            // Configure Map Fields action
            //

            // Reconfigure Map Fields to have it pick up upstream fields
            var mapFieldsAction = this.solution.ChildrenActions.Single(a => a.Name == "Map Fields");
            mapFieldsAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/configure", mapFieldsAction);

            // Configure mappings
            crateStorage = _crate.FromDto(mapFieldsAction.CrateStorage);
            controlsCrate = crateStorage.CratesOfType<StandardConfigurationControlsCM>().First();
            var mapping = controlsCrate.Content.Controls.OfType<MappingPane>().First();
            mapping.Value = @"[{""Key"":""Doctor"",""Value"":""Doctor""},{""Key"":""Condition"",""Value"":""Condition""}]";

            using (var updater = _crate.UpdateStorage(mapFieldsAction))
            {
                updater.CrateStorage.Remove<StandardConfigurationControlsCM>();
                updater.CrateStorage.Add(controlsCrate);
            }
            sendEnvelopeAction = await HttpPostAsync<ActivityDTO, ActivityDTO>(baseUrl + "actions/save", mapFieldsAction);

            //
            // Activate and run plan
            //
            await HttpPostAsync<string, string>(baseUrl + "routes/run?planId=" + plan.Id, null);

            //
            // Deactivate plan
            //
            //await HttpPostAsync<string, string>(baseUrl + "routes/deactivate?id=" + plan.Id, plan);

            //
            // Delete plan
            //
            await HttpDeleteAsync(baseUrl + "routes?id=" + plan.Id);

            // Verify that test email has been received
            EmailAssert.EmailReceived("dse_demo@docusign.net", "Test Message from Fr8");
        }
    }
}
