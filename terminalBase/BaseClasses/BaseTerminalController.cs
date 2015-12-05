﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using StructureMap;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using TerminalBase.Infrastructure;
using Utilities.Configuration.Azure;

namespace TerminalBase.BaseClasses
{

    //this is a quasi base class. We can't use inheritance directly because it's across project boundaries, but
    //we can generate instances of this.
    public class BaseTerminalController : ApiController
    {
        private readonly BaseTerminalEvent _baseTerminalEvent;
        public BaseTerminalController()
        {
            _baseTerminalEvent = new BaseTerminalEvent();
        }

        /// <summary>
        /// Reports Terminal Error incident
        /// </summary>
        [HttpGet]
        public IHttpActionResult ReportTerminalError(string terminalName, Exception terminalError)
        {
            var exceptionMessage = terminalError.ToString();//string.Format("{0}\r\n{1}", terminalError.Message, terminalError.StackTrace);
            return Json(_baseTerminalEvent.SendTerminalErrorIncident(terminalName, exceptionMessage, terminalError.GetType().Name));
        }

        /// <summary>
        /// Reports start up incident
        /// </summary>
        /// <param name="terminalName">Name of the terminal which is starting up</param>
        [HttpGet]
        public IHttpActionResult AfterStartup(string terminalName)
        {
            return null;

            //TODO: Commented during development only. So that app loads fast.
            //return Json(ReportStartUp(terminalName));
        }

        /// <summary>
        /// Reports start up event by making a Post request
        /// </summary>
        /// <param name="terminalName"></param>
        private Task<string> ReportStartUp(string terminalName)
        {
            return _baseTerminalEvent.SendEventOrIncidentReport(terminalName, "Terminal Incident");
        }


        private void BindTestHubCommunicator(object curObject)
        {
            var baseTerminalAction = curObject as BaseTerminalAction;

            if (baseTerminalAction == null)
            {
                return;
            }

            baseTerminalAction.HubCommunicator = new TestMonitoringHubCommunicator();
        }

        private void BindExplicitDataHubCommunicator(object curObject)
        {
            var baseTerminalAction = curObject as BaseTerminalAction;

            if (baseTerminalAction == null)
            {
                return;
            }

            baseTerminalAction.HubCommunicator = new ExplicitDataHubCommunicator();
        }
        
        /// <summary>
        /// Reports event when process an action
        /// </summary>
        /// <param name="terminalName"></param>
        private Task<string> ReportEvent(string terminalName)
        {
            return _baseTerminalEvent.SendEventOrIncidentReport(terminalName, "Terminal Event");
        }

        // For /Configure and /Activate actions that accept ActionDTO
        public async Task<object> HandleFr8Request(string curTerminal, string curActionPath, ActionDTO curActionDTO)
        {
            if (curActionDTO == null)
                throw new ArgumentNullException("curActionDTO");
            if (curActionDTO.ActivityTemplate == null)
                throw new ArgumentException("ActivityTemplate is null", "curActionDTO");

            var isTestActivityTemplate = false;
            var activityTemplateName = curActionDTO.ActivityTemplate.Name;
            if (activityTemplateName.EndsWith("_TEST"))
            {
                isTestActivityTemplate = true;
                activityTemplateName = activityTemplateName
                    .Substring(0, activityTemplateName.Length - "_TEST".Length);
            }

            string curAssemblyName = string.Format("{0}.Actions.{1}_v{2}", curTerminal, activityTemplateName, curActionDTO.ActivityTemplate.Version);

            Type calledType = Type.GetType(curAssemblyName + ", " + curTerminal);
            if (calledType == null)
                throw new ArgumentException(string.Format("Action {0}_v{1} doesn't exist in {2} terminal.",
                    curActionDTO.ActivityTemplate.Name,
                    curActionDTO.ActivityTemplate.Version,
                    curTerminal), "curActionDTO");
            MethodInfo curMethodInfo = calledType.GetMethod(curActionPath, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            object curObject = Activator.CreateInstance(calledType);

            if (isTestActivityTemplate)
            {
                BindTestHubCommunicator(curObject);
            }
            else if (curActionDTO.IsExplicitData)
            {
                BindExplicitDataHubCommunicator(curObject);
            }

            var curActionDO = Mapper.Map<ActionDO>(curActionDTO);

            var curAuthTokenDO = Mapper.Map<AuthorizationTokenDO>(curActionDTO.AuthToken);
            var curContainerId = curActionDTO.ContainerId;
            Task<ActionDO> response;
            switch (curActionPath.ToLower())
            {
                case "configure":
                    {
                        Task<ActionDO> resutlActionDO = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO, curAuthTokenDO });
                        return await resutlActionDO.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result));
                    }
                case "run":
                    {
                        Task<PayloadDTO> resultPayloadDTO = (Task<PayloadDTO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO, curContainerId, curAuthTokenDO });
                        return await resultPayloadDTO;
                    }
                case "initialconfigurationresponse":
                    {
                        Task<ActionDO> resutlActionDO = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO, curAuthTokenDO });
                        return await resutlActionDO.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result));
                    }
                case "followupconfigurationresponse":
                    {
                        Task<ActionDO> resutlActionDO = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO, curAuthTokenDO });
                        return await resutlActionDO.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result));
                    }
                case "Activate":
                    {
                        Task<ActionDO> resutlActionDO;

                        var param = curMethodInfo.GetParameters();
                        if (param.Length == 2)
                            resutlActionDO = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO, curAuthTokenDO });
                        else
                        {
                            response = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO });
                            return await response.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result)); ;
                        }

                        return resutlActionDO.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result));
                    }
                case "Deactivate":
                    {
                        Task<ActionDO> resutlActionDO;

                        var param = curMethodInfo.GetParameters();
                        if(param.Length == 2)
                            resutlActionDO = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO, curAuthTokenDO });
                        else
                        {
                            response = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO });
                            return await response.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result)); ;
                        }

                        return resutlActionDO.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result));
                    }
                default:
                    response = (Task<ActionDO>)curMethodInfo.Invoke(curObject, new Object[] { curActionDO });
                    return await response.ContinueWith(x => Mapper.Map<ActionDTO>(x.Result)); ;

            }


        }
    }
}
