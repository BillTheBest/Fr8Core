﻿using terminalSalesforce.Infrastructure;
using terminalSalesforce.Services;
using TerminalBase.Infrastructure;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace terminalSalesforce.Controllers
{
    [RoutePrefix("terminals/terminalSalesforce")]
    public class EventController : ApiController
    {
        private IEvent _event;
        private BaseTerminalEvent _baseTerminalEvent;

        public EventController()
        {
            _event = ObjectFactory.GetInstance<IEvent>();
            _baseTerminalEvent = new BaseTerminalEvent();
        }

        [HttpPost]
        [Route("events")]
        public async Task<string> ProcessIncomingNotification()
        {
            string eventPayLoadContent = Request.Content.ReadAsStringAsync().Result;
            await _baseTerminalEvent.Process(eventPayLoadContent, _event.ProcessEvent);

            //We need to acknowledge the request from Salesforce
            //Creating a SOAP XML response to acknowledge
            string response = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
                            xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                             <soapenv:Body>
                              <notificationsResponse xmlns=""http://soap.sforce.com/2005/09/outbound"">
                                     <Ack>true</Ack>
                                  </notificationsResponse>
                              </soapenv:Body>
                            </soapenv:Envelope>";
            return response;
        }
    }
}