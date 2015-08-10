﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AutoMapper;
using Core.Interfaces;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using Microsoft.Ajax.Utilities;
using StructureMap;
using Web.Controllers.Helpers;
using Web.ViewModels;

namespace Web.Controllers
{
    [Authorize]
    public class ProcessTemplateController : ApiController
    {
        private readonly IProcessTemplate _processTemplate;

        public ProcessTemplateController()
            : this(ObjectFactory.GetInstance<IProcessTemplate>())
        {
            
        }

        public ProcessTemplateController(IProcessTemplate processTemplate)
        {
            _processTemplate = processTemplate;
        }

        // GET api/<controller>
        public IHttpActionResult Get(int? id = null)
        {

            var curProcessTemplates = _processTemplate.GetForUser(User.Identity.Name, User.IsInRole(Roles.Admin),id);

            switch (curProcessTemplates.Count)
            {
                case 0:
                    throw new ApplicationException("Process Template(s) not found for "+ (id != null ? "id {0}".FormatInvariant(id) :"the current user"));
                case 1:
                    return Ok(Mapper.Map<ProcessTemplateDTO>(curProcessTemplates.First()));
            }

            return Ok(curProcessTemplates.Select(Mapper.Map<ProcessTemplateDTO>));
            
        }

        

        public IHttpActionResult Post(ProcessTemplateDTO processTemplateDto)
        {

            if (string.IsNullOrEmpty(processTemplateDto.Name))
            {
                ModelState.AddModelError("Name", "Name cannot be null");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Some of the request data is invalid");
            }

            var curProcessTemplateDO = Mapper.Map<ProcessTemplateDTO, ProcessTemplateDO>(processTemplateDto);
            curProcessTemplateDO.UserId = User.Identity.Name;
            processTemplateDto.Id = _processTemplate.CreateOrUpdate(curProcessTemplateDO);

            return Ok(processTemplateDto);
        }

        public IHttpActionResult Delete(int id)
        {
            _processTemplate.Delete(id);
            return Ok(id);
        }

        
    }
}