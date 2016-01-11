﻿using System;
using System.Collections.Generic;
using System.Web.Http.Dispatcher;
using Microsoft.Owin;
using Owin;
using terminalDocuSign;
using terminalDocuSign.Controllers;
using terminalDocuSign.Infrastructure.AutoMapper;
using TerminalBase.BaseClasses;

[assembly: OwinStartup(typeof(Startup))]

namespace terminalDocuSign
{
    public class Startup : BaseConfiguration
    {
        public void Configuration(IAppBuilder app)
        {
            Configuration(app, false);
        }

        public void Configuration(IAppBuilder app, bool selfHost)
        {
            ConfigureProject(selfHost, TerminalDocusignStructureMapBootstrapper.LiveConfiguration);
            TerminalDataAutoMapperBootStrapper.ConfigureAutoMapper();
            RoutesConfig.Register(_configuration);
            ConfigureFormatters();

            app.UseWebApi(_configuration);

            if (!selfHost)
            {
                StartHosting("terminalDocuSign");
            }
        }

        public override ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            return new Type[] {
                    typeof(ActionController),
                    typeof(EventController),
                    typeof(TerminalController),
                    typeof(AuthenticationController)
                };
        }
    }
}
