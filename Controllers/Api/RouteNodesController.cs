﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using AutoMapper;
using HubWeb.Infrastructure;
using Microsoft.AspNet.Identity;
using StructureMap;
using Data.Infrastructure.StructureMap;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Hub.Interfaces;
using Hub.Managers;
using Data.Crates;
using Data.Interfaces.Manifests;
using Data.States;
using Data.Constants;

namespace HubWeb.Controllers
{
    //[RoutePrefix("route_nodes")]
    public class RouteNodesController : ApiController
    {
        private readonly IRouteNode _activity;
        private readonly ISecurityServices _security;
        private readonly ICrateManager _crate;
        private readonly IActivityTemplate _activityTemplate;

        public RouteNodesController()
        {
            _activity = ObjectFactory.GetInstance<IRouteNode>();
            _security = ObjectFactory.GetInstance<ISecurityServices>();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
            _activityTemplate = ObjectFactory.GetInstance<IActivityTemplate>();
        }

        [HttpGet]
        [ResponseType(typeof (ActivityTemplateDTO))]
        [Fr8ApiAuthorize]
        public IHttpActionResult Get(int id)
        {
            var curActivityTemplateDO = _activityTemplate.GetByKey(id);
            var curActivityTemplateDTO = Mapper.Map<ActivityTemplateDTO>(curActivityTemplateDO);

            return Ok(curActivityTemplateDTO);
        }

        [ActionName("upstream")]
        [ResponseType(typeof (List<RouteNodeDO>))]
        [Fr8ApiAuthorize]
        public IHttpActionResult GetUpstreamActivities(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityDO = uow.PlanRepository.GetById<ActivityDO>(id);
                var upstreamActivities = _activity.GetUpstreamActivities(uow, activityDO);
                return Ok(upstreamActivities);
            }
        }

        [ActionName("downstream")]
        [ResponseType(typeof (List<RouteNodeDO>))]
        [Fr8ApiAuthorize]
        public IHttpActionResult GetDownstreamActivities(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                ActivityDO activityDO = uow.PlanRepository.GetById<ActivityDO>(id);
                var downstreamActivities = _activity.GetDownstreamActivities(uow, activityDO);
                return Ok(downstreamActivities);
            }
        }

        // TODO: after DO-1214 is completed, this method must be removed.
        [ActionName("upstream_actions")]
        [ResponseType(typeof (List<ActivityDTO>))]
        [Fr8HubWebHMACAuthenticate]
        public IHttpActionResult GetUpstreamActions(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var activityDO = uow.PlanRepository.GetById<ActivityDO>(id);
                var upstreamActions = _activity
                    .GetUpstreamActivities(uow, activityDO)
                    .OfType<ActivityDO>()
                    .Select(x => Mapper.Map<ActivityDTO>(x))
                    .ToList();

                return Ok(upstreamActions);
            }
        }
        // TODO: after DO-1214 is completed, this method must be removed.
        [ActionName("downstream_actions")]
        [ResponseType(typeof (List<ActivityDTO>))]
        [Fr8HubWebHMACAuthenticate]
        public IHttpActionResult GetDownstreamActions(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                ActivityDO activityDO = uow.PlanRepository.GetById<ActivityDO>(id);
                var downstreamActions = _activity
                    .GetDownstreamActivities(uow, activityDO)
                    .OfType<ActivityDO>()
                    .Select(x => Mapper.Map<ActivityDTO>(x))
                    .ToList();

                return Ok(downstreamActions);
            }
        }

        [ActionName("designtime_fields_dir")]
        [ResponseType(typeof(StandardDesignTimeFieldsCM))]
        [Fr8HubWebHMACAuthenticate]
        public IHttpActionResult GetDesignTimeFieldsByDirection(
            Guid id, 
            CrateDirection direction, 
            AvailabilityType availability = AvailabilityType.NotSet)
        {
            var downstreamActions = _activity.GetDesignTimeFieldsByDirection(id, direction, availability);
            return Ok(downstreamActions);
        }

        [ActionName("available")]
        [ResponseType(typeof (IEnumerable<ActivityTemplateCategoryDTO>))]
        [AllowAnonymous]
        [HttpGet]
        public IHttpActionResult GetAvailableActivities()
        {
            var categoriesWithActivities = _activity.GetAvailableActivityGroups();

            return Ok(categoriesWithActivities);
        }

        [ActionName("available")]
        [ResponseType(typeof (IEnumerable<ActivityTemplateDTO>))]
        [AllowAnonymous]
        [HttpGet]
        public IHttpActionResult GetAvailableActivities(string tag)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                Func<ActivityTemplateDO, bool> predicate = (at) =>
                    string.IsNullOrEmpty(at.Tags) ? false :
                        at.Tags.Split(new char[] {','}).Any(c => string.Equals(c.Trim(), tag, StringComparison.InvariantCultureIgnoreCase));
                var categoriesWithActivities = _activity.GetAvailableActivities(uow, tag == "[all]" ? (at) => true : predicate);
                return Ok(categoriesWithActivities);
            }
        }
    }
}