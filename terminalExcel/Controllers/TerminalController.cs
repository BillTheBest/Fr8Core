﻿using System.Collections.Generic;
using System.Web.Http.Description;
using System.Web.Http;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Utilities.Configuration.Azure;
using Utilities.Configuration.Azure;
using Data.Interfaces.Manifests;

namespace terminalExcel.Controllers
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

            var terminal = new TerminalDTO
            {
                Endpoint = CloudConfigurationManager.GetSetting("TerminalEndpoint"),
                TerminalStatus = TerminalStatus.Active,
                Name = "terminalExcel",
                Version = "1"
            };

	        var webService = new WebServiceDTO
	        {
				Name = "Excel"
	        };

            result.Add(new ActivityTemplateDTO
            {
                Name = "Load_Excel_File",
                Label = "Load Excel File",
                Version = "1",
                Category = ActivityCategory.Receivers.ToString(),
                Terminal = terminal,
                Tags = "Table Data Generator",
                MinPaneWidth = 210,
				WebService = webService
            });


            StandardFr8TerminalCM curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = terminal,
                Actions = result
            };
            return Json(curStandardFr8TerminalCM);
        }
    }
}