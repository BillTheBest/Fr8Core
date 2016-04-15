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
    public class PlanNodesController : ApiController
    {
        private readonly IPlanNode _activity;
        private readonly ISecurityServices _security;
        private readonly ICrateManager _crate;
        private readonly IActivityTemplate _activityTemplate;

        public PlanNodesController()
        {
            _activity = ObjectFactory.GetInstance<IPlanNode>();
            _security = ObjectFactory.GetInstance<ISecurityServices>();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
            _activityTemplate = ObjectFactory.GetInstance<IActivityTemplate>();
        }

        [HttpGet]
        [ResponseType(typeof(ActivityTemplateDTO))]
        [Fr8ApiAuthorize]
        public IHttpActionResult Get(int id)
        {
            var curActivityTemplateDO = _activityTemplate.GetByKey(id);
            var curActivityTemplateDTO = Mapper.Map<ActivityTemplateDTO>(curActivityTemplateDO);

            return Ok(curActivityTemplateDTO);
        }

        [ActionName("upstream")]
        [ResponseType(typeof(List<PlanNodeDO>))]
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
        [ResponseType(typeof(List<PlanNodeDO>))]
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
        [ResponseType(typeof(List<ActivityDTO>))]
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
        [ResponseType(typeof(List<ActivityDTO>))]
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

        [HttpGet]
        [ActionName("upstream_fields")]
        [Fr8ApiAuthorize]
        public IHttpActionResult ExtractUpstream(
            Guid id,
            string manifestType,
            AvailabilityType availability = AvailabilityType.NotSet)
        {
            CrateManifestType cmt;
            if (!ManifestDiscovery.Default.TryResolveManifestType(manifestType, out cmt))
            {
                return BadRequest();
            }

            Type type;
            if (!ManifestDiscovery.Default.TryResolveType(cmt, out type))
            {
                return BadRequest();
            }

            var method = typeof(IPlanNode)
                .GetMethod("GetCrateManifestsByDirection")
                .MakeGenericMethod(type);

            var data = method.Invoke(_activity, new object[] { id, CrateDirection.Upstream, availability, false });

            return Ok(data);
        }

        [HttpGet]
        [ActionName("available_data")]
        [Fr8ApiAuthorize]
        public IHttpActionResult GetAvailableData(Guid id, CrateDirection crateDirection, AvailabilityType availability)
        {
            return Ok(_activity.GetAvailableData(id, crateDirection, availability));
        }
        
        [ActionName("available")]
        [ResponseType(typeof(IEnumerable<ActivityTemplateCategoryDTO>))]
        [AllowAnonymous]
        [HttpGet]
        public IHttpActionResult GetAvailableActivities()
        {
            var categoriesWithActivities = _activity.GetAvailableActivityGroups();

            return Ok(categoriesWithActivities);
        }

        [ActionName("available")]
        [ResponseType(typeof(IEnumerable<ActivityTemplateDTO>))]
        [AllowAnonymous]
        [HttpGet]
        public IHttpActionResult GetAvailableActivities(string tag)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                Func<ActivityTemplateDO, bool> predicate = (at) =>
                    string.IsNullOrEmpty(at.Tags) ? false :
                        at.Tags.Split(new char[] { ',' }).Any(c => string.Equals(c.Trim(), tag, StringComparison.InvariantCultureIgnoreCase));
                var categoriesWithActivities = _activity.GetAvailableActivities(uow, tag == "[all]" ? (at) => true : predicate);
                return Ok(categoriesWithActivities);
            }
        }
    }
}