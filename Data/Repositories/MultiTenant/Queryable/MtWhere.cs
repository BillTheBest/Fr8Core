﻿using System;
using System.Linq.Expressions;
using Data.Interfaces.Manifests;

namespace Data.Repositories.MultiTenant.Queryable
{
    public class MtWhere<T> : MtQueryable<T>
        where T : Manifest
    {
        public readonly Expression<Func<T, bool>> Condition;

        public MtWhere(IMtQueryable<T> prev, Expression<Func<T, bool>> condition)
            : base(prev)
        {
            Condition = condition;
        }
    }
}