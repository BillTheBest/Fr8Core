﻿using System.Collections.Generic;
using Data.Interfaces.DataTransferObjects;

namespace Data.Interfaces.ManifestSchemas
{
    public class EventReportMS
    {
        public string EventNames { get; set; }
        public string ProcessDOId { get; set; }
        public string ExternalAccountId { get; set; }
        public List<CrateDTO> EventPayload { get; set; }

        public EventReportMS()
        {
            EventPayload = new List<CrateDTO>();
        }
}
}
