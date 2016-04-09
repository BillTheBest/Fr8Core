﻿using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Filters;
using Newtonsoft.Json;
using StructureMap;
using Data.Interfaces.DataTransferObjects;
using Hub.Exceptions;
using Hub.Managers;
using TerminalBase;
using Utilities;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using System.Collections.Generic;
using System.Linq;
using Data.Infrastructure;

namespace HubWeb.ExceptionHandling
{
    /// <summary>
    /// This exception filter handles any non-handled exception. Usually for such 
    /// exceptions we can provide no specific or useful instructions to user. 
    /// </summary>
    public class WebApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            //if (context.Exception is TaskCanceledException)
            //{
            //    // TaskCanceledException is an exception representing a successful task cancellation 
            //    // Don't need to log it
            //    // Ref: https://msdn.microsoft.com/en-us/library/dd997396(v=vs.110).aspx
            //    return;
            //}

            ErrorDTO errorDto;

            // Collect error messages of all inner exceptions

            var alertManager = ObjectFactory.GetInstance<EventReporter>();
            var ex = context.Exception;

            //Post exception information to AppInsights
            Dictionary<string, string> properties = new Dictionary<string, string>();
            foreach (KeyValuePair<string, object> arg in context.ActionContext.ActionArguments)
            {
                properties.Add(arg.Key, JsonConvert.SerializeObject(arg.Value));
            }
            new TelemetryClient().TrackException(ex, properties);

            alertManager.UnhandledErrorCaught(
                String.Format("Unhandled exception has occurred.\r\nError message: {0}\r\nCall stack:\r\n{1}",
                ex.GetFullExceptionMessage(),
                ex.StackTrace));

            if (ex.GetType() == typeof (HttpException))
            {
                var httpException = (HttpException) ex;
                context.Response = new HttpResponseMessage((HttpStatusCode) httpException.GetHttpCode());
            }
            else
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
            
            if (ex is AuthenticationExeception)
            {
                errorDto = ErrorDTO.AuthenticationError();
            }
            else
            {
                errorDto = ErrorDTO.InternalError();
            }

            errorDto.Message = "Sorry, an unexpected error has occurred while serving your request. Please try again in a few minutes.";
           
            // if debugging enabled send back the details of exception as well
            if (HttpContext.Current.IsDebuggingEnabled)
            {
                if (ex is TerminalCodedException) 
                {
                    var terminalEx = (TerminalCodedException)ex;
                    
                    errorDto.Details = new
                    {
                        errorCode = terminalEx.ErrorCode, 
                        message = terminalEx.ErrorCode.GetEnumDescription()
                    };
                }
                else 
                {
                    errorDto.Details = new {exception = context.Exception};
                }
            }
            
            context.Response.Content = new StringContent(JsonConvert.SerializeObject(errorDto), Encoding.UTF8, "application/json");
        }
    }
}