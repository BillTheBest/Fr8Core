﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces;

namespace Hub.Interfaces
{
    public interface IReport
    {
        List<FactDO> GetAllFacts(IUnitOfWork uow);
        List<IncidentDO> GetAllIncidents(IUnitOfWork uow);
    }
}
