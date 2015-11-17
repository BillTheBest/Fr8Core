﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using AutoMapper;
using Microsoft.AspNet.Identity;
using StructureMap;
// This alias is used to avoid ambiguity between StructureMap.IContainer and Core.Interfaces.IContainer
using Utilities;
using InternalInterface = Hub.Interfaces;
using Data.Entities;
using Data.Infrastructure;
using Data.Infrastructure.StructureMap;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Interfaces;
using Hub.Managers;
using Hub.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HubWeb.Controllers
{
    // commented out by yakov.gnusin.
    // Please DO NOT put [Fr8ApiAuthorize] on class, this breaks process execution!
    // [Fr8ApiAuthorize]
    [RoutePrefix("api/containers")]
    public class ContainerController : ApiController
    {
        private readonly InternalInterface.IContainer _container;
        private readonly ISecurityServices _security;
       // private readonly ICrateManager _crateManager;

        public ContainerController()
        {
            _container = ObjectFactory.GetInstance<InternalInterface.IContainer>();
            _security = ObjectFactory.GetInstance<ISecurityServices>();
      //      _crateManager = ObjectFactory.GetInstance<ICrateManager>();
        }

        [HttpGet]
        [Route("{id:guid}")]
        public IHttpActionResult Get(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curContainerDO = uow.ContainerRepository.GetByKey(id);
                var curPayloadDTO = new PayloadDTO(id);

                if (curContainerDO.CrateStorage == null)
                {
                    curContainerDO.CrateStorage = string.Empty;
                }

                curPayloadDTO.CrateStorage = JsonConvert.DeserializeObject<CrateStorageDTO>(curContainerDO.CrateStorage);

                EventManager.ProcessRequestReceived(curContainerDO);

                return Ok(curPayloadDTO);
            }
        }

        [Fr8ApiAuthorize]
        [Route("getIdsByName")]
        [HttpGet]
        public IHttpActionResult GetIdsByName(string name)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var containerIds = uow.ContainerRepository.GetQuery().Where(x => x.Name == name).Select(x => x.Id).ToArray();

                return Json(containerIds);
            }
        }

        [Fr8ApiAuthorize]
        [Route("launch")]
        [HttpPost]
        public async Task<IHttpActionResult> Launch(int routeId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var processTemplateDO = uow.RouteRepository.GetByKey(routeId);
                var pusherNotifier = new PusherNotifier();
                try
                {
                    var containerDO = await _container.Launch(processTemplateDO, null);
                    pusherNotifier.Notify(String.Format("fr8pusher_{0}", User.Identity.Name),
                        "fr8pusher_container_executed", String.Format("Route \"{0}\" executed", processTemplateDO.Name));

                    return Ok(Mapper.Map<ContainerDTO>(containerDO));
                }
                catch
                {
                    pusherNotifier.Notify(String.Format("fr8pusher_{0}", User.Identity.Name),
                        "fr8pusher_container_failed", String.Format("Route \"{0}\" failed", processTemplateDO.Name));
                }

                return Ok();
            }
        }

        // Return the Containers accordingly to ID given
        [Fr8ApiAuthorize]
        [Route("get/{id:guid?}")]
        [HttpGet]
        public IHttpActionResult Get(Guid? id = null)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IList<ContainerDO> curContainer = _container
                    .GetByFr8Account(
                        uow,
                        _security.GetCurrentAccount(uow),
                        _security.IsCurrentUserHasRole(Roles.Admin),
                        id
                    );

                if (curContainer.Any())
                {
                    if (id.HasValue)
                    {
                        return Ok(Mapper.Map<ContainerDTO>(curContainer.First()));
                    }

                    return Ok(curContainer.Select(Mapper.Map<ContainerDTO>));
                }
                return Ok();
            }
        }

      
        //NOTE: IF AND WHEN THIS CLASS GETS USED, IT NEEDS TO BE FIXED TO USE OUR 
        //STANDARD UOW APPROACH, AND NOT CONTACT THE DATABASE TABLE DIRECTLY.

        //private DockyardDbContext db = new DockyardDbContext();
        // GET: api/Process
        //public IQueryable<ProcessDO> Get()
        //{
        //    return db.Processes;
        //}

        //// GET: api/Process/5
        //[ResponseType(typeof(ProcessDO))]
        //public IHttpActionResult GetProcess(int id)
        //{
        //    ProcessDO processDO = db.Processes.Find(id);
        //    if (processDO == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(processDO);
        //}

        //// PUT: api/Process/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutProcess(int id, ProcessDO processDO)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != processDO.Id)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(processDO).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!ProcessDOExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        //// POST: api/Process
        //[ResponseType(typeof(ProcessDO))]
        //public IHttpActionResult PostProcessDO(ProcessDO processDO)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.Processes.Add(processDO);
        //    db.SaveChanges();

        //    return CreatedAtRoute("DefaultApi", new { id = processDO.Id }, processDO);
        //}

        //// DELETE: api/Process/5
        //[ResponseType(typeof(ProcessDO))]
        //public IHttpActionResult DeleteProcessDO(int id)
        //{
        //    ProcessDO processDO = db.Processes.Find(id);
        //    if (processDO == null)
        //    {
        //        return NotFound();
        //    }

        //    db.Processes.Remove(processDO);
        //    db.SaveChanges();

        //    return Ok(processDO);
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        //private bool ProcessDOExists(int id)
        //{
        //    return db.Processes.Count(e => e.Id == id) > 0;
        //}
    }
}