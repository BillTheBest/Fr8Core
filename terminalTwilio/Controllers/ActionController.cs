<<<<<<< HEAD
﻿using System;
=======
﻿using System.Threading.Tasks;
>>>>>>> dev
using System.Web.Http;
using Data.Interfaces.DataTransferObjects;
using TerminalBase.BaseClasses;

namespace terminalTwilio.Controllers
{    
    [RoutePrefix("actions")]
    public class ActionController : ApiController
    {
        private const string curTerminal = "terminalTwilio";
        private readonly BaseTerminalController _baseTerminalController = new BaseTerminalController();

        [HttpPost]
        public Task<object> Execute([FromUri] String actionType, [FromBody] ActionDTO curActionDTO)
        {
            return _baseTerminalController.HandleFr8Request(curTerminal, actionType, curActionDTO);
        }
    }
}