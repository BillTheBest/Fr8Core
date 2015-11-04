﻿using Data.Interfaces;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Newtonsoft.Json;
using StructureMap;
using System.Collections.Generic;
using System;
using System.Linq;
using Data.Crates;
using Data.Interfaces.Manifests;

namespace UtilitiesTesting.Fixtures
{
    partial class FixtureData
    {


        public static List<Crate<StandardDesignTimeFieldsCM>> TestCrateDTO1()
        {
            List<FieldDTO> fields = new List<FieldDTO>();
            fields.Add(new FieldDTO() { Key = "Medical_Form_v1", Value = Guid.NewGuid().ToString() });
            fields.Add(new FieldDTO() { Key = "Medical_Form_v2", Value = Guid.NewGuid().ToString() });

            return new List<Crate<StandardDesignTimeFieldsCM>>() { Crate<StandardDesignTimeFieldsCM>.FromContent("Available Templates", new StandardDesignTimeFieldsCM() { Fields = fields }) };
        }

        public static List<Crate> TestCrateDTO2()
        {
            List<FieldDTO> fields = new List<FieldDTO>();
            
            fields.Add(new FieldDTO() { Key = "Text 5", Value = Guid.NewGuid().ToString() });
            fields.Add(new FieldDTO() { Key = "Text 8", Value = Guid.NewGuid().ToString() });
            fields.Add(new FieldDTO() { Key = "Doctor", Value = Guid.NewGuid().ToString() });
            fields.Add(new FieldDTO() { Key = "Condition", Value = Guid.NewGuid().ToString() });
            
            return new List<Crate>() { Crate.FromContent("DocuSignTemplateUserDefinedFields", new StandardDesignTimeFieldsCM() { Fields = fields }) };

        }


        public static Crate CreateStandardConfigurationControls()
        {
            string templateId = "58521204-58af-4e65-8a77-4f4b51fef626";
            var fieldSelectDocusignTemplate = new DropDownListControlDefinitionDTO()
            {
                Label = "Select DocuSign Template",
                Name = "Selected_DocuSign_Template",
                Required = true,
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                },
                Source = new FieldSourceDTO
                {
                    Label = "Available Templates",
                    ManifestType = CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME
                },
                Value = templateId
            };

            var fieldEnvelopeSent = new CheckBoxControlDefinitionDTO()
            {
                Label = "Envelope Sent",
                Name = "Event_Envelope_Sent"
            };

            var fieldEnvelopeReceived = new CheckBoxControlDefinitionDTO()
            {
                Label = "Envelope Received",
                Name = "Event_Envelope_Received"
            };

            var fieldRecipientSigned = new CheckBoxControlDefinitionDTO()
            {
                Label = "Recipient Signed",
                Name = "Event_Recipient_Signed"
            };

            var fieldEventRecipientSent = new CheckBoxControlDefinitionDTO()
            {
                Label = "Recipient Sent",
                Name = "Event_Recipient_Sent"
            };

            
            return PackControlsCrate(
                fieldSelectDocusignTemplate,
                fieldEnvelopeSent,
                fieldEnvelopeReceived,
                fieldRecipientSigned,
                fieldEventRecipientSent);
        }

        #region Private Methods

        private static Crate PackControlsCrate(params ControlDefinitionDTO[] controlsList)
        {
            return Crate.FromContent("Configuration_Controls", new StandardConfigurationControlsCM(controlsList));
        }

        private static Crate CreateStandardConfigurationControlsCrate(string label, params ControlDefinitionDTO[] controls)
        {
            return Crate.FromContent(label, new StandardConfigurationControlsCM(controls));
        }

//        private static CrateDTO Create(string label, string contents, string manifestType = "", int manifestId = 0)
//        {
//            var crateDTO = new CrateDTO()
//            {
//                Id = Guid.NewGuid().ToString(),
//                Label = label,
//                Contents = contents,
//                ManifestType = manifestType,
//                ManifestId = manifestId
//            };
//            return crateDTO;
//        }



        private static Crate CreateEventSubscriptionCrate(StandardConfigurationControlsCM configurationFields)
        {
            var subscriptions = new List<string>();

            var eventCheckBoxes = configurationFields.Controls
                .Where(x => x.Type == "checkboxField" && x.Name.StartsWith("Event_"));

            foreach (var eventCheckBox in eventCheckBoxes)
            {
                if (eventCheckBox.Selected)
                {
                    subscriptions.Add(eventCheckBox.Label);
                }
            }

            return CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                subscriptions.ToArray()
                );
        }

        private static Crate CreateStandardEventSubscriptionsCrate(string label, params string[] subscriptions)
        {
            return Crate.FromContent(label, new EventSubscriptionCM() {Subscriptions = subscriptions.ToList()});
        }

        #endregion


    }
}