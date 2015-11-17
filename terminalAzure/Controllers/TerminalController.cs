﻿using System;
using System.Collections.Generic;
using System.Web.Http.Description;
using System.Web.Http;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Utilities.Configuration.Azure;
using Data.Interfaces.Manifests;

namespace terminalAzure.Controllers
{
    [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        /// <summary>
        /// Terminal discovery infrastructure.
        /// Action returns list of supported actions by terminal.
        /// </summary>
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(StandardFr8TerminalCM))]
        public IHttpActionResult DiscoverTerminals()
        {
            var result = new List<ActivityTemplateDTO>();

            var template = new ActivityTemplateDTO
            {
                Name = "Write_To_Sql_Server",
                Label = "Write to Azure Sql Server",
                Category = ActivityCategory.Forwarders.ToString(),
                AuthenticationType = AuthenticationType.None,
                Version = "1",
                MinPaneWidth = 330
            };

            var terminal = new TerminalDTO
            {
                Endpoint = CloudConfigurationManager.GetSetting("TerminalEndpoint"),
                TerminalStatus = TerminalStatus.Active,
                Name = "terminalAzure",
                Version = "1"
            };

            var webService = new WebServiceDO
            {
                Name = "Microsoft Azure"
            };


	        template.WebServiceName = webService.Name;

            result.Add(template);

            StandardFr8TerminalCM curStandardFr8TerminalCM = new StandardFr8TerminalCM()
             {
                 Definition = terminal,
                 Actions = result
             };

            return Json(curStandardFr8TerminalCM);
        }
    }
}