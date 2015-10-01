﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Utilities.Serializers.Json;
using pluginDocuSign.Infrastructure;
using pluginDocuSign.Interfaces;

namespace pluginDocuSign.Services
{
    public class DocuSignEnvelope : DocuSign.Integrations.Client.Envelope, IDocuSignEnvelope
    {
        private string _baseUrl;
        private readonly ITab _tab;
        private readonly ISigner _signer;
        //Can't use DockYardAccount here - circular dependency
        private readonly DocuSignPackager _docuSignPackager;

        public DocuSignEnvelope()
        {
            //TODO change baseUrl later. Remove it to constructor parameter etc.
            _baseUrl = string.Empty;

            //TODO move ioc container.
            _tab = new Tab();
            _signer = new Signer();

            _docuSignPackager = new DocuSignPackager
            {
                CurrentEmail = ConfigurationManager.AppSettings["DocuSignLoginEmail"],
                CurrentApiPassword = ConfigurationManager.AppSettings["DocuSignLoginPassword"]
            };
            Login = _docuSignPackager.Login();
        }


        /// <summary>
        /// Get Envelope Data from a docusign envelope. 
        /// Each EnvelopeData row is essentially a specific DocuSign "Tab".
        /// List of Envelope Data.
        /// It returns empty list of envelope data if tab and signers not found.
        /// </summary>
        public IList<EnvelopeDataDTO> GetEnvelopeData(string curEnvelopeId)
        {
            if (string.IsNullOrEmpty(curEnvelopeId))
            {
                throw new ArgumentNullException("envelopeId");
            }
            EnvelopeId = curEnvelopeId;
            GetRecipients(true, true);
            return GetEnvelopeData(this);
        }

        // TODO: This implementation of the interface method is no different than what is already implemented in the other overload. Hence commenting out here and in the interface definition.
        // If not deleted, this will cause grief as DocuSingEnvelope (object in parameter) is defined in both the plugin project and the Data project  and interface expects it to be in Data.Wrappers 
        // namespace, where it will not belong. 
        //public List<EnvelopeDataDTO> GetEnvelopeData(DocuSignEnvelope envelope)
        //{
        //    Signer[] curSignersSet = _signer.GetFromRecipients(envelope);
        //    if (curSignersSet != null)
        //    {
        //        foreach (var curSigner in curSignersSet)
        //        {
        //            return _tab.ExtractEnvelopeData(envelope, curSigner);
        //        }
        //    }

        //    return new List<EnvelopeDataDTO>();
        //}


        /// <summary>
        /// Get Envelope Data from a docusign envelope. 
        /// Each EnvelopeData row is essentially a specific DocuSign "Tab".
        /// </summary>
        /// <param name="envelope">DocuSign.Integrations.Client.Envelope envelope.</param>
        /// <returns>
        /// List of Envelope Data.
        /// It returns empty list of envelope data if tab and signers not found.
        /// </returns>
        public IList<EnvelopeDataDTO> GetEnvelopeData(DocuSign.Integrations.Client.Envelope envelope)
        {
            Signer[] curSignersSet = _signer.GetFromRecipients(envelope);
            if (curSignersSet != null)
            {
                foreach (var curSigner in curSignersSet)
                {
                    return _tab.ExtractEnvelopeData(envelope, curSigner);
                }
            }
            return new List<EnvelopeDataDTO>();
        }

        /// <summary>
        /// Creates payload as a collection of fields based on field mappings created by user 
        /// and field values retrieved from a DocuSign envelope.
        /// </summary>
        /// <param name="curFields">Field mappings created by user for an action.</param>
        /// <param name="curEnvelopeId">Envelope id which is being processed.</param>
        /// <param name="curEnvelopeData">A collection of form fields extracted from the DocuSign envelope.</param>
        public IList<FieldDTO> ExtractPayload(List<FieldDTO> curFields, string curEnvelopeId,
            IList<EnvelopeDataDTO> curEnvelopeData)
        {
            var payload = new List<FieldDTO>();

            if (curFields != null)
            {
                curFields.ForEach(f =>
                {
                    var newValue = curEnvelopeData.Where(e => e.Name == f.Key).Select(e => e.Value).SingleOrDefault();
                    if (newValue == null)
                    {
                        EventManager.DocuSignFieldMissing(curEnvelopeId, f.Key);
                    }
                    else
                    {
                        payload.Add(new FieldDTO() { Key = f.Key, Value = newValue });
                    }
                });
            }
            return payload;
        }

        public IEnumerable<EnvelopeDataDTO> GetEnvelopeDataByTemplate(string templateId)
        {
            var curDocuSignTemplate = new DocuSignTemplate();

            var templateDetails = curDocuSignTemplate.GetTemplate(templateId);
            foreach (var signer in templateDetails["recipients"]["signers"])
            {
                if (signer["tabs"]["textTabs"] != null)
                    foreach (var textTab in signer["tabs"]["textTabs"])
                    {
                        yield return CreateEnvelopeData(textTab, textTab["value"].ToString());
                    }
                if (signer["tabs"]["checkboxTabs"] == null) continue;
                foreach (var chekBoxTabs in signer["tabs"]["checkboxTabs"])
                {
                    yield return CreateEnvelopeData(chekBoxTabs, chekBoxTabs["selected"].ToString());
                }
            }
        }

        private EnvelopeDataDTO CreateEnvelopeData(dynamic tab, string value)
        {
            return new EnvelopeDataDTO
            {
                DocumentId = tab.documentId,
                RecipientId = tab.recipientId,
                Name = tab.tabLabel,
                TabId = tab.tabId,
                Value = value,
                Type = GetFieldType((string)tab.name)
            };
        }

        private string GetFieldType(string name)
        {
            switch (name)
            {
                case "Checkbox":
                    return FieldDefinitionDTO.CHECKBOX_FIELD;
                case "Text":
                    return FieldDefinitionDTO.TEXTBOX_FIELD;
                default:
                    return FieldDefinitionDTO.TEXTBOX_FIELD;
            }
        }
    }
}