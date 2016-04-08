﻿
using System.Data.Entity;
using System.Linq;
using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;

namespace HubWeb.Controllers.Helpers
{
    public static class PlanMappingHelper
    {
        // Manual mapping method to resolve DO-1164.
        public static PlanDTO MapPlanToDto(IUnitOfWork uow, PlanDO curPlanDO)
        {
            var subPlanDTOList = curPlanDO.ChildNodes.OfType<SubPlanDO>()
                .OrderBy(x => x.Ordering)
                .ToList()
                .Select((SubPlanDO x) =>
                {
                    var pntDTO = Mapper.Map<FullSubPlanDTO>(x);

                    pntDTO.Activities = x.ChildNodes.OrderBy(y => y.Ordering).Select(Mapper.Map<ActivityDTO>).ToList();

                    return pntDTO;
                }).ToList();


            var result = new PlanDTO()
            {
                Plan = new PlanFullDTO()
                {
                    Description = curPlanDO.Description,
                    Id = curPlanDO.Id,
                    Name = curPlanDO.Name,
                    PlanState = curPlanDO.PlanState,
                    Visibility = curPlanDO.Visibility,
                    StartingSubPlanId = curPlanDO.StartingSubPlanId,
                    SubPlans = subPlanDTOList,
                    Fr8UserId = curPlanDO.Fr8AccountId,
                    Tag = curPlanDO.Tag,
                    Category = curPlanDO.Category,
                    LastUpdated = curPlanDO.LastUpdated
                }

            };

            return result;
        }
    }
}