﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Interfaces;
using StructureMap;
using terminalDocuSign.Interfaces;

namespace terminalDocuSign.Services
{
    /// <summary>
    /// Service to create DocuSign related routes in Hub
    /// </summary>
    public class DocuSignRoute : IDocuSignRoute
    {
        private readonly IActivityTemplate _activityTemplate;
        private readonly IAction _action;

        public DocuSignRoute()
        {
            _activityTemplate = ObjectFactory.GetInstance<IActivityTemplate>();
            _action = ObjectFactory.GetInstance<IAction>();
        }

        /// <summary>
        /// Creates Monitor All DocuSign Events route with Record DocuSign Events and Store MT Data actions.
        /// </summary>
        public async Task CreateRoute_MonitorAllDocuSignEvents(string curFr8UserId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {

                var curFr8Account = uow.UserRepository.GetByKey(curFr8UserId);

                //if route already created
                if (uow.RouteRepository.GetQuery().Any(existingRoute =>
                    existingRoute.Name.Equals("MonitorAllDocuSignEvents") &&
                    existingRoute.Fr8Account.Email.Equals(curFr8Account.Email) &&
                    existingRoute.RouteState == RouteState.Active))
                {
                    return;
                }

                //Create a route
                RouteDO route = new RouteDO
                {
                    Name = "MonitorAllDocuSignEvents",
                    Description = "Monitor All DocuSign Events",
                    Fr8Account = curFr8Account,
                    RouteState = RouteState.Active,
                    Tag = "Monitor",
                    Id = Guid.NewGuid()
                };

                //create a subroute
                var subroute = new SubrouteDO(true)
                {
                    ParentRouteNode = route,
                    Id = Guid.NewGuid()
                };

                //update Route and Subroute into database
                route.ChildNodes = new List<RouteNodeDO> { subroute };
                uow.RouteNodeRepository.Add(route);
                uow.RouteNodeRepository.Add(subroute);
                uow.SaveChanges();

                //get activity templates of required actions
                var activity1 = Mapper.Map<ActivityTemplateDTO>(_activityTemplate.GetByName(uow, "Record_DocuSign_Events_v1"));
                var activity2 = Mapper.Map<ActivityTemplateDTO>(_activityTemplate.GetByName(uow, "StoreMTData_v1"));

                //create and configure required actions
                await _action.CreateAndConfigure(uow, curFr8UserId, activity1.Id, activity1.Name, activity1.Label, subroute.Id);
                await _action.CreateAndConfigure(uow, curFr8UserId, activity2.Id, activity2.Name, activity2.Label, subroute.Id);
                
                //update database
                uow.SaveChanges();
            }
        }
    }
}