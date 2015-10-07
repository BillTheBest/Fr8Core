﻿using System;
using System.Collections.Generic;
using System.Web.Http.Description;
using System.Web.Http;
using Data.Entities;
using Data.States;

namespace terminal_AzureSqlServer.Controllers
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
        [ResponseType(typeof(List<ActivityTemplateDO>))]
        public IHttpActionResult DiscoverTerminals()
        {
            var result = new List<ActivityTemplateDO>();
            
            var template = new ActivityTemplateDO
            {
                Name = "Write_To_Sql_Server",
                Category = ActivityCategory.fr8_Forwarder,
                Version = "1"
            };

            var terminal = new PluginDO
            {
                Endpoint = "localhost:46281",
                PluginStatus = PluginStatus.Active,
                Name = "terminal_AzureSqlServer",
                RequiresAuthentication = false,
                Version = "1"
            };
            
            template.Plugin = terminal;

            result.Add(template);

            return Json(result);    
        }
    }
}