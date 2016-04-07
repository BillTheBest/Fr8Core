﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Managers;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Services;
using TerminalBase.Infrastructure;
using TerminalBase.Infrastructure.Behaviors;
using terminalDocuSign.Services.New_Api;
using terminalDocuSign.Actions;
using DocuSign.eSign.Api;
using DocuSign.eSign.Model;

namespace terminalDocuSign.Actions
{
    public class Process_Personal_Report_v1 : Send_DocuSign_Envelope_v1
    {
        string Tab1 = "{      \"height\": 11,      \"validationPattern\": \"\",      \"validationMessage\": \"\",      \"shared\": \"false\",      \"requireInitialOnSharedChange\": \"false\",      \"requireAll\": \"false\",      \"name\": \"Text\",      \"value\": \"\",      \"originalValue\": \"\",      \"width\": 120,      \"required\": \"true\",      \"locked\": \"false\",      \"concealValueOnDocument\": \"false\",      \"disableAutoSize\": \"true\",      \"tabLabel\": \"Data Field 3\",      \"documentId\": \"1\",      \"recipientId\": \"1\",      \"pageNumber\": \"1\",      \"xPosition\": \"75\",      \"yPosition\": \"223\",      \"tabId\": \"051c659c-f109-4ed9-9bcc-8573323f3cd4\",      \"templateLocked\": \"false\",      \"templateRequired\": \"false\"    }";
        string checkBox1 = "{      \"name\": \"Checkbox\",      \"tabLabel\": \"Check Box 5\",      \"selected\": \"false\",      \"shared\": \"false\",      \"requireInitialOnSharedChange\": \"false\",      \"required\": \"false\",      \"locked\": \"false\",      \"documentId\": \"1\",      \"recipientId\": \"1\",      \"pageNumber\": \"1\",      \"xPosition\": \"286\",      \"yPosition\": \"223\",      \"tabId\": \"c928c7d4-9ec9-4414-a0a0-9c1ad01eb2be\",      \"templateLocked\": \"false\",      \"templateRequired\": \"false\"    }";
        string Tab2 = " {      \"height\": 11,      \"validationPattern\": \"\",      \"validationMessage\": \"\",      \"shared\": \"false\",      \"requireInitialOnSharedChange\": \"false\",      \"requireAll\": \"false\",      \"name\": \"Text\",      \"value\": \"\",      \"width\": 42,      \"required\": \"true\",      \"locked\": \"false\",      \"concealValueOnDocument\": \"false\",      \"disableAutoSize\": \"false\",      \"tabLabel\": \"Data Field 6\",      \"documentId\": \"1\",      \"recipientId\": \"1\",      \"pageNumber\": \"1\",      \"xPosition\": \"416\",      \"yPosition\": \"223\",      \"tabId\": \"ffcf95e1-dd25-46b8-95b4-567eb7d19ba9\",      \"templateLocked\": \"false\",      \"templateRequired\": \"false\"    }";


        protected override string ActivityUserFriendlyName => "Process Personal Report";

        protected override PayloadDTO SendAnEnvelope(ICrateStorage curStorage, DocuSignApiConfiguration loginInfo, PayloadDTO payloadCrates,
            List<FieldDTO> rolesList, List<FieldDTO> fieldList, string curTemplateId)
        {
            try
            {
                var mttb = FindControl(curStorage, "mltb");
                string[] names = mttb.Value.Split(new string[] { "\n", ",", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                SendPersonalEnvelope(curStorage, loginInfo, payloadCrates, rolesList, fieldList, curTemplateId, names);
            }
            catch (Exception ex)
            {
                return Error(payloadCrates, $"Couldn't send an envelope. {ex}");
            }
            return Success(payloadCrates);
        }

        protected async override Task<Crate> CreateDocusignTemplateConfigurationControls(ActivityDO curActivity)
        {

            var multiLineTB = new TextBoxBig()
            {
                Label = "Volunteer Names",
                Name = "mltb"
            };

            var fieldSelectDocusignTemplateDTO = new DropDownList
            {
                Label = "Use DocuSign Template",
                Name = "target_docusign_template",
                Required = true,
                Events = new List<ControlEvent>()
                {
                     ControlEvent.RequestConfig
                },
                Source = null
            };

            var fieldsDTO = new List<ControlDefinitionDTO>
            {
                multiLineTB, fieldSelectDocusignTemplateDTO
            };

            var controls = new StandardConfigurationControlsCM
            {
                Controls = fieldsDTO
            };

            return CrateManager.CreateStandardConfigurationControlsCrate("Configuration_Controls", fieldsDTO.ToArray());
        }


        private void SendPersonalEnvelope(ICrateStorage curStorage, DocuSignApiConfiguration loginInfo, PayloadDTO payloadCrates,
            List<FieldDTO> rolesList, List<FieldDTO> fieldList, string curTemplateId, string[] names)
        {

            var templatesApi = new TemplatesApi(loginInfo.Configuration);
            EnvelopesApi envelopesApi = new EnvelopesApi(loginInfo.Configuration);

            var template = templatesApi.ListTemplates(loginInfo.AccountId).EnvelopeTemplates.Where(x => x.Name.Contains("Personnel")).FirstOrDefault();



            EnvelopeDefinition envDef = new EnvelopeDefinition();
            envDef.EmailSubject = "Test message from Fr8";
            envDef.TemplateId = template.TemplateId;


            envDef.Status = "created";

            var summ = envelopesApi.CreateEnvelope(loginInfo.AccountId, envDef);

            var recipientId = envelopesApi.ListRecipients(loginInfo.AccountId, summ.EnvelopeId).Signers[0].RecipientId;
            var documentId = envelopesApi.ListDocuments(loginInfo.AccountId, summ.EnvelopeId).EnvelopeDocuments[0].DocumentId;
            var tabs = envelopesApi.ListTabs(loginInfo.AccountId, summ.EnvelopeId, recipientId);



            tabs.TextTabs = new List<Text>();
            tabs.CheckboxTabs = new List<Checkbox>();

            int i = 0;

            foreach (var person in names)
            {
                var textTab1 = JsonConvert.DeserializeObject<Text>(Tab1);
                var checkBox = JsonConvert.DeserializeObject<Checkbox>(checkBox1);
                var textTab2 = JsonConvert.DeserializeObject<Text>(Tab2);

                textTab1.YPosition = UpdateHeight(textTab1.YPosition, i);
                checkBox.YPosition = UpdateHeight(checkBox.YPosition, i);
                textTab2.YPosition = UpdateHeight(textTab2.YPosition, i);
                textTab1.Value = person;
                textTab1.TabId = "";
                checkBox.TabId = "";
                textTab2.TabId = "";

                tabs.TextTabs.Add(textTab1);
                tabs.TextTabs.Add(textTab2);
                tabs.CheckboxTabs.Add(checkBox);

                i++;
            }



            envelopesApi.CreateTabs(loginInfo.AccountId, summ.EnvelopeId, recipientId, tabs);
            // sending an envelope
            envelopesApi.Update(loginInfo.AccountId, summ.EnvelopeId, new Envelope() { Status = "sent" });
        }

        string UpdateHeight(string h, int i)
        {
            int height = Convert.ToInt32(h);
            height += (20 * i);
            return height.ToString();
        }
    }
}