﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces;

namespace Core.Interfaces
{
	public interface IActivity
	{
		List<ActivityDO> GetUpstreamActivities(IUnitOfWork uow, ActivityDO curActivityDO);
        List<ActivityDO> GetDownstreamActivities(IUnitOfWork uow, ActivityDO curActivityDO);
        Task Process(int curActivityId, ProcessDO curProcessDO);
        IEnumerable<ActivityTemplateDO> GetAvailableActivities(IUnitOfWork uow, IDockyardAccountDO curAccount);
        ActivityDO GetNextActivity(ActivityDO currentActivity, ActivityDO root);
	    void Delete(IUnitOfWork uow, ActivityDO activity);
	}
}