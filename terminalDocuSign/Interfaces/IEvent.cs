﻿using System.Threading.Tasks;
using Data.Crates;

namespace terminalDocuSign.Interfaces
{
    public interface IEvent
    {
        /// <summary>
        /// Processes external event payload from the Terminal
        /// </summary>
        Task<Crate> Process(string curExternalEventPayload);
    }
}