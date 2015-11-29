﻿using System.Web.Http;
using TerminalBase.BaseClasses;

namespace terminalTwilio
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            BaseTerminalWebApiConfig.Register(config);
            config.Routes.MapHttpRoute("TerminalTwilio", "terminal_twilio/{controller}/{id}"
           );
        }
    }
}
