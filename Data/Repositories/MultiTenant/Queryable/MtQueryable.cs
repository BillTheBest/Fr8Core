﻿using System.Collections;
using System.Collections.Generic;
using Data.Interfaces.Manifests;

namespace Data.Repositories
{
    public abstract class MtQueryable<T> : IMtQueryable<T>
        where T: Manifest
    {
        public IMtQueryable<T> Previous { get; private set; }
        public IMtQueryExecutor<T> Executor { get; private set; }

        protected MtQueryable(IMtQueryable<T> prev)
        {
            Previous = prev;
            Executor = prev.Executor;
        }

        protected MtQueryable(IMtQueryable<T> previous, IMtQueryExecutor<T> executor)
        {
            Previous = previous;
            Executor = executor;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Executor.RunQuery(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}