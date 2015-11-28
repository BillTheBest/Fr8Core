﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using DocuSign.Integrations.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utilities.AutoMapper;
using Signer = DocuSign.Integrations.Client.Signer;

namespace Data.Infrastructure.AutoMapper
{
    public class DataAutoMapperBootStrapper
    {
        public static void ConfigureAutoMapper()
        {
            Mapper.CreateMap<ActionNameDTO, ActivityTemplateDO>()
                .ForMember(activityTemplateDO => activityTemplateDO.Name, opts => opts.ResolveUsing(e => e.Name))
                .ForMember(activityTemplateDO => activityTemplateDO.Version, opts => opts.ResolveUsing(e => e.Version));


            Mapper.CreateMap<ActionDO, ActionDTO>();

            Mapper.CreateMap<Fr8AccountDO, UserDTO>()
                .ForMember(dto => dto.EmailAddress, opts => opts.ResolveUsing(e => e.EmailAddress.Address))
                .ForMember(dto => dto.Status, opts => opts.ResolveUsing(e => e.State.Value));

            Mapper.CreateMap<WebServiceDO, WebServiceDTO>();
            Mapper.CreateMap<WebServiceDTO, WebServiceDO>();
            Mapper.CreateMap<string, JToken>().ConvertUsing<StringToJTokenConverter>();
            Mapper.CreateMap<JToken, string>().ConvertUsing<JTokenToStringConverter>();

            Mapper.CreateMap<ActionDO, ActionDTO>().ForMember(a => a.Id, opts => opts.ResolveUsing(ad => ad.Id))
                .ForMember(a => a.Name, opts => opts.ResolveUsing(ad => ad.Name))
                .ForMember(a => a.ParentRouteNodeId, opts => opts.ResolveUsing(ad => ad.ParentRouteNodeId))
                //.ForMember(a => a.CrateStorage, opts => opts.ResolveUsing(ad => ad.CrateStorage == null ? null : JsonConvert.DeserializeObject(ad.CrateStorage)))
                .ForMember(a => a.ActivityTemplateId, opts => opts.ResolveUsing(ad => ad.ActivityTemplateId))
                .ForMember(a => a.CurrentView, opts => opts.ResolveUsing(ad => ad.currentView))
                .ForMember(a => a.ChildrenActions, opts => opts.ResolveUsing(ad => ad.ChildNodes.OfType<ActionDO>()))
                .ForMember(a => a.ActivityTemplate, opts => opts.ResolveUsing(ad => ad.ActivityTemplate));
                

            Mapper.CreateMap<ActionDTO, ActionDO>().ForMember(a => a.Id, opts => opts.ResolveUsing(ad => ad.Id))
                .ForMember(a => a.Name, opts => opts.ResolveUsing(ad => ad.Name))
                .ForMember(a => a.ParentRouteNodeId, opts => opts.ResolveUsing(ad => ad.ParentRouteNodeId))
                .ForMember(a => a.ActivityTemplateId, opts => opts.ResolveUsing(ad => ad.ActivityTemplateId))
                .ForMember(a => a.ActivityTemplate, opts => opts.ResolveUsing(ad => ad.ActivityTemplate))
                //.ForMember(a => a.CrateStorage, opts => opts.ResolveUsing(ad => Newtonsoft.Json.JsonConvert.SerializeObject(ad.CrateStorage)))
                .ForMember(a => a.currentView, opts => opts.ResolveUsing(ad => ad.CurrentView))
                .ForMember(a => a.ChildNodes, opts => opts.ResolveUsing(ad => MapActions(ad.ChildrenActions)))
                .ForMember(a => a.IsTempId, opts => opts.ResolveUsing(ad => ad.IsTempId));


            Mapper.CreateMap<ActivityTemplateDO, ActivityTemplateDTO>()
                .ForMember(x => x.Id, opts => opts.ResolveUsing(x => x.Id))
                .ForMember(x => x.Name, opts => opts.ResolveUsing(x => x.Name))
                .ForMember(x => x.Version, opts => opts.ResolveUsing(x => x.Version))
                .ForMember(x => x.Description, opts => opts.ResolveUsing(x => x.Description))
                .ForMember(x => x.TerminalId, opts => opts.ResolveUsing(x => x.TerminalId)); ;

            Mapper.CreateMap<ActivityTemplateDTO, ActivityTemplateDO>()
                .ForMember(x => x.Id, opts => opts.ResolveUsing(x => x.Id))
                .ForMember(x => x.Name, opts => opts.ResolveUsing(x => x.Name))
                .ForMember(x => x.ComponentActivities, opts => opts.ResolveUsing(x => x.ComponentActivities))
                .ForMember(x => x.Version, opts => opts.ResolveUsing(x => x.Version))
                .ForMember(x => x.TerminalId, opts => opts.ResolveUsing(x => x.TerminalId))
                .ForMember(x => x.Terminal, opts => opts.ResolveUsing(x => x.Terminal))
                .ForMember(x => x.AuthenticationType, opts => opts.ResolveUsing(x => x.AuthenticationType))
                .ForMember(x => x.WebService, opts => opts.ResolveUsing(x => Mapper.Map<WebServiceDO>(x.WebService)))
                .ForMember(x => x.AuthenticationTypeTemplate, opts => opts.ResolveUsing((ActivityTemplateDTO x) => null))
                .ForMember(x => x.ActivityTemplateStateTemplate,
                    opts => opts.ResolveUsing((ActivityTemplateDTO x) => null))
                .ForMember(x => x.WebServiceId, opts => opts.ResolveUsing((ActivityTemplateDTO x) => null)) 
                .ForMember(x => x.Description, opts => opts.ResolveUsing(x => x.Description));

//
//            Mapper.CreateMap<ActionListDO, ActionListDTO>()
//                .ForMember(x => x.Id, opts => opts.ResolveUsing(x => x.Id))
//                .ForMember(x => x.ActionListType, opts => opts.ResolveUsing(x => x.ActionListType))
//                .ForMember(x => x.Name, opts => opts.ResolveUsing(x => x.Name));

            Mapper.CreateMap<RouteDO, RouteEmptyDTO>();
            Mapper.CreateMap<RouteEmptyDTO, RouteDO>();
            Mapper.CreateMap<RouteDO, RouteEmptyDTO>();
            Mapper.CreateMap<SubrouteDTO, SubrouteDO>()
                .ForMember(x => x.ParentRouteNodeId, opts => opts.ResolveUsing(x => x.RouteId));
            Mapper.CreateMap<SubrouteDO, SubrouteDTO>()
                .ForMember(x => x.RouteId, opts => opts.ResolveUsing(x => x.ParentRouteNodeId));

            Mapper.CreateMap<CriteriaDO, CriteriaDTO>()
                .ForMember(x => x.Conditions, opts => opts.ResolveUsing(y => y.ConditionsJSON));
            Mapper.CreateMap<CriteriaDTO, CriteriaDO>()
                .ForMember(x => x.ConditionsJSON, opts => opts.ResolveUsing(y => y.Conditions));

            Mapper.CreateMap<RouteDO, RouteFullDTO>()
                .ConvertUsing<RouteDOFullConverter>();

            Mapper.CreateMap<RouteEmptyDTO, RouteFullDTO>();
          //  Mapper.CreateMap<ActionListDO, FullActionListDTO>();
            Mapper.CreateMap<SubrouteDO, FullSubrouteDTO>();

            //Mapper.CreateMap<Account, DocuSignAccount>();
            Mapper.CreateMap<FileDO, FileDescriptionDTO>();
            
            Mapper.CreateMap<CrateStorageDTO, string>()
                .ConvertUsing<JsonToStringConverterNoMagic<CrateStorageDTO>>();
            Mapper.CreateMap<string, CrateStorageDTO>()
                .ConvertUsing<CrateStorageFromStringConverter>();
            Mapper.CreateMap<FileDO, FileDTO>();

            Mapper.CreateMap<ContainerDO, ContainerDTO>();
            Mapper.CreateMap<AuthorizationTokenDTO, AuthorizationTokenDO>();
            Mapper.CreateMap<TerminalDO, TerminalDTO>();
            Mapper.CreateMap<TerminalDTO, TerminalDO>();

        }

        private static List<RouteNodeDO> MapActions(IEnumerable<ActionDTO> actions)
        {
            var list = new List<RouteNodeDO>();

            if (actions != null)
            {
                foreach (var actionDto in actions)
                {
                    list.Add(Mapper.Map<ActionDO>(actionDto));
                }
            }

            return list;
        }
    }   
}