﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using StructureMap;

namespace HubWeb.Controllers
{
	[RoutePrefix("webservices")]
	public class WebServicesController : ApiController
	{
	    private const string UknownWebServiceName = "UnknownService";

		[HttpGet]
		[Route("")]
		public IHttpActionResult GetWebServices()
		{
			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				var models = uow.WebServiceRepository.GetAll()
					.Select(x => Mapper.Map<WebServiceDTO>(x))
					.ToList();

				return Ok(models);
			}
		}

		[HttpPost]
		[Route("")]
		public IHttpActionResult CreateWebService(WebServiceDTO webService)
		{
			WebServiceDO entity = Mapper.Map<WebServiceDO>(webService);

			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				uow.WebServiceRepository.Add(entity);

				uow.SaveChanges();
			}

			var model = Mapper.Map<WebServiceDTO>(entity);

			return Ok(model);
		}

		[HttpPost]
		[Route("actions")]
		public IHttpActionResult GetActions(ActivityCategory[] categories)
		{
			List<WebServiceActionSetDTO> webServiceList;

			using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
			{
				// Getting web services and their actions as one set, then filtering that set
				// to get only those actions whose category matches any of categories provided
				// resulting set is grouped into batches 1 x web service - n x actions

                var templates = uow.ActivityTemplateRepository.GetQuery().Include(x => x.WebService).ToArray();
                var unknwonService = uow.WebServiceRepository.GetQuery().FirstOrDefault(x => x.Name == UknownWebServiceName);

			    webServiceList = templates.Where(x => categories == null || categories.Contains(x.Category))
			        .GroupBy(x => x.WebService, x => x, (key, group) => new
			        {
			            WebService = key,
			            SortOrder = key == null ? 1 : 0,
			            Actions = group
			        }).OrderBy(x => x.SortOrder)
			        .Select(x => new WebServiceActionSetDTO
			        {
			            WebServiceIconPath = x.WebService != null ? x.WebService.IconPath : (unknwonService != null ? unknwonService.IconPath : null),
			            Actions = x.Actions.Select(p => new ActivityTemplateDTO
			            {
			                Id = p.Id,
			                Name = p.Name,
			                Category = p.Category.ToString(),
			                ComponentActivities = p.ComponentActivities,
			                Label = p.Label,
			                MinPaneWidth = p.MinPaneWidth,
			                TerminalId = p.Terminal.Id,
			                Version = p.Version
			            }).ToList()
			        }).ToList();
			}

			return Ok(webServiceList);
		}
	}
}