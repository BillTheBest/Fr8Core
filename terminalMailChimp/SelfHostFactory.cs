﻿using System;
using Microsoft.Owin.Hosting;
using Owin;

namespace terminalMailChimp
{
    public class SelfHostFactory
    {
        public class SelfHostStartup
        {
            public void Configuration(IAppBuilder app)
            {
                var startup = new Startup();
                startup.Configuration(app, selfHost: true);
            }
        }

        public static IDisposable CreateServer(string url)
        {
            return WebApp.Start<SelfHostStartup>(url: url);
        }
    }
}