﻿using System.Web.Http;
using Data.Infrastructure.AutoMapper;
using Hub.StructureMap;
using terminalPapertrail.Tests.Infrastructure;
using DependencyType = Hub.StructureMap.StructureMapBootStrapper.DependencyType;

namespace terminalPapertrail
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //var formatters = GlobalConfiguration.Configuration.Formatters;
            //formatters.Remove(formatters.XmlFormatter);

            GlobalConfiguration.Configure(WebApiConfig.Register);
			DataAutoMapperBootStrapper.ConfigureAutoMapper();
            //TerminalDataAutoMapperBootStrapper.ConfigureAutoMapper();
            // StructureMap Dependencies configuration
            StructureMapBootStrapper.ConfigureDependencies(DependencyType.LIVE);
            TerminalPapertrailMapBootstrapper.ConfigureDependencies(DependencyType.LIVE);
        }
    }
}
