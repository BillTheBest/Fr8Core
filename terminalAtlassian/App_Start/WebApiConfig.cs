﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using TerminalBase.BaseClasses;

namespace terminalAtlassian
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            BaseTerminalWebApiConfig.Register("DropBox", config);
        }
    }
}
