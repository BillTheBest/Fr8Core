﻿using System.Web.Http;
using TerminalBase;
using System.Web;
using System.Web.Routing;

namespace TerminalBase.BaseClasses
{
    public static class BaseTerminalWebApiConfig
    {
        public static void Register(string curTerminalName, HttpConfiguration curTerminalConfiguration)
        {
            
            var name = string.Format("terminal_{0}", curTerminalName);
            //map attribute routes
            curTerminalConfiguration.MapHttpAttributeRoutes();

            curTerminalConfiguration.Routes.MapHttpRoute(
                name: name,
                routeTemplate: string.Format("terminal_{0}", curTerminalName) + "/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            curTerminalConfiguration.Routes.MapHttpRoute(
                name: string.Format("Terminal{0}ActionCatchAll", curTerminalName),
                routeTemplate: "actions/{*actionType}",
                defaults: new { controller = "Action", action = "Execute", terminal = name }); //It calls ActionController#Execute in an MVC style
            
            //add Web API Exception Filter
            curTerminalConfiguration.Filters.Add(new WebApiExceptionFilterAttribute());
        }
        public static void RegisterRoutes(string curTerminalName, RouteCollection curTerminalRoutes)
        {

            //curTerminalRoutes.MapHttpRoute(
            //    name: string.Format("Terminal{0}Route", curTerminalName),
            //    routeTemplate: string.Format("terminal_{0}", curTerminalName) + "/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);
            //curTerminalRoutes.MapHttpRoute(
            //    name: string.Format("Terminal{0}RouteActionCatchAll", curTerminalName),
            //    routeTemplate: "actions/{*actionType}",
            //    defaults: new { controller = "Action", action = "Execute" }); //It calls ActionController#Execute in an MVC style
        }
    }
}
