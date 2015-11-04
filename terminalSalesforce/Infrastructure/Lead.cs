﻿using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Hub.Managers;
using StructureMap;

namespace terminalSalesforce.Infrastructure
{
    public class Lead
    {
        ForceClient client;
        private ICrateManager _crateManager;


        public Lead()
        {
            _crateManager = ObjectFactory.GetInstance<ICrateManager>();
        }


        public async Task CreateLead(ActionDTO currentActionDTO)
        {
            
            string instanceUrl, apiVersion;
            ParseAuthToken(currentActionDTO.AuthToken.AdditionalAttributes,out instanceUrl,out apiVersion);
            client = new ForceClient(instanceUrl, currentActionDTO.AuthToken.Token, apiVersion);
            LeadDTO lead = new LeadDTO();


            var storage = _crateManager.GetStorage(currentActionDTO);

            var curFieldList = storage.CrateValuesOfType<StandardConfigurationControlsCM>().First();

            lead.FirstName = curFieldList.Controls.First(x => x.Name == "firstName").Value;
            lead.LastName = curFieldList.Controls.First(x => x.Name == "lastName").Value;
            lead.Company = curFieldList.Controls.First(x => x.Name == "companyName").Value;
            if (!String.IsNullOrEmpty(lead.LastName) && !String.IsNullOrEmpty(lead.Company))
            {
                var newLeadId = await client.CreateAsync("Lead", lead);
            }
        }

        public void ParseAuthToken(string authonTokenAdditionalValues,out string instanceUrl,out string apiVersion)
        {
            int startIndexOfInstanceUrl = authonTokenAdditionalValues.IndexOf("instance_url");
            int startIndexOfApiVersion = authonTokenAdditionalValues.IndexOf("api_version");
            instanceUrl = authonTokenAdditionalValues.Substring(startIndexOfInstanceUrl, (startIndexOfApiVersion - 1 - startIndexOfInstanceUrl));
            apiVersion = authonTokenAdditionalValues.Substring(startIndexOfApiVersion, authonTokenAdditionalValues.Length - startIndexOfApiVersion);
            instanceUrl = instanceUrl.Replace("instance_url=", "");
            apiVersion = apiVersion.Replace("api_version=", "");
        }



    }
}