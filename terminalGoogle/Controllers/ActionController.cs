﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using Data.Interfaces.DataTransferObjects;
using TerminalBase.BaseClasses;

namespace terminalGoogle.Controllers
{
    [RoutePrefix("actions/terminal_Google")]
    public class ActionController : ApiController
    {
        private const string curTerminal = "terminalGoogle";
        private BaseTerminalController _baseTerminalController = new BaseTerminalController();

        [HttpPost]
        public Task<object> Execute([FromUri] String actionType, [FromBody] ActionDTO curActionDTO)
        {
            return _baseTerminalController.HandleFr8Request(curTerminal, actionType, curActionDTO);
        }
    }
}