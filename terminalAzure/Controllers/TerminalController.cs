﻿using System.Web.Http.Description;
using System.Web.Http;
using Fr8Data.Manifests;
using TerminalBase;
using TerminalBase.BaseClasses;
using TerminalBase.Services;

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
            StandardFr8TerminalCM curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                 Definition = TerminalData.TerminalDTO,
                 Activities = ActivityStore.GetAllActivities()
            };

            return Json(curStandardFr8TerminalCM);
        }
    }
}