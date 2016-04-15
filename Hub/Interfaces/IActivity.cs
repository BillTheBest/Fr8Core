using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Constants;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;


namespace Hub.Interfaces
{
    public interface IActivity
    {
        IEnumerable<TViewModel> GetAllActivities<TViewModel>();
        Task<ActivityDTO> SaveOrUpdateActivity(ActivityDO currentActivityDo);
        Task<ActivityDTO> Configure(IUnitOfWork uow, string userId, ActivityDO curActivityDO, bool saveResult = true);
        ActivityDO GetById(IUnitOfWork uow, Guid id);
        ActivityDO MapFromDTO(ActivityDTO curActivityDTO);

        Task<PlanNodeDO> CreateAndConfigure(IUnitOfWork uow, string userId, int actionTemplateId, 
                                             string label = null, int? order = null, Guid? parentNodeId = null, bool createPlan = false, Guid? authorizationTokenId = null);

        Task<PayloadDTO> Run(IUnitOfWork uow, ActivityDO curActivityDO, ActivityExecutionMode curActionExecutionMode, ContainerDO curContainerDO);
        Task<ActivityDTO> Activate(ActivityDO curActivityDO);
        Task<ActivityDTO> Deactivate(ActivityDO curActivityDO);
        Task<T> GetActivityDocumentation<T>(ActivityDTO curActivityDTO, bool isSolution = false) where T : class;
        List<string> GetSolutionNameList(string terminalName);
        void Delete(Guid id);
    }
}