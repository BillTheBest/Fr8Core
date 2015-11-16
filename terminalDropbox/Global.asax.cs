﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;
using Data.Infrastructure.AutoMapper;

namespace terminalDropbox
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            DataAutoMapperBootStrapper.ConfigureAutoMapper();
            // StructureMap Dependencies configuration
            Hub.StructureMap.StructureMapBootStrapper.ConfigureDependencies(Hub.StructureMap.StructureMapBootStrapper.DependencyType.LIVE);          
        }
    }
}