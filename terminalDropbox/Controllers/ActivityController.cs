﻿using System;
using System.Web.Http;
using Data.Interfaces.DataTransferObjects;
using TerminalBase.BaseClasses;
using System.Threading.Tasks;
using TerminalBase.Infrastructure;

namespace terminalDropbox.Controllers
{
    [RoutePrefix("activities")]
    public class ActivityController: BaseTerminalController
    {
        private const string curTerminal = "terminalDropbox";

        [HttpPost]
        [fr8TerminalHMACAuthenticate(curTerminal)]
        [Authorize]
        public Task<object> Execute([FromUri] String actionType, [FromBody] Fr8DataDTO curDataDTO)
        {
            return HandleFr8Request(curTerminal, actionType, curDataDTO);
        }
    }
}