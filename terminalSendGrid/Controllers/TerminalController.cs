﻿using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Utilities.Configuration.Azure;

namespace terminalSendGrid.Controllers
{
    [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(StandardFr8TerminalCM))]
        public IHttpActionResult DiscoverTerminals()
        {
            var terminal = new TerminalDTO()
            {
                Name = "terminalSendGrid",
                TerminalStatus = TerminalStatus.Active,
                Endpoint = CloudConfigurationManager.GetSetting("TerminalEndpoint"),
                Version = "1"
            };

	        var webService = new WebServiceDTO
	        {
		        Name = "SendGrid"
	        };

            var action = new ActivityTemplateDTO()
            {
                Name = "SendEmailViaSendGrid",
                Label = "Send Email Via Send Grid",
                Version = "1",
                Description = "Send Email Via Send Grid: Description",
                Tags = "Notifier",
                Terminal = terminal,
                AuthenticationType = AuthenticationType.None,
                Category = ActivityCategory.Forwarders,
                MinPaneWidth = 330,
                WebService = webService
            };

            var actionList = new List<ActivityTemplateDTO>()
            {
                action
            };

            StandardFr8TerminalCM curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = terminal,
                Actions = actionList
            };
            return Json(curStandardFr8TerminalCM);
        }
    }
}