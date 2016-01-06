﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json;

namespace terminalSalesforce.Infrastructure
{
    /// <summary>
    /// Filter execution type.
    /// </summary>
    public enum FilterExecutionType
    {
        WithFilter = 1,
        WithoutFilter = 2
    }

    /// <summary>
    /// DTO for deserializing data from FilterPane form when Executing action.
    /// </summary>
    public class FilterDataDTO
    {
        [JsonProperty("executionType")]
        public FilterExecutionType ExecutionType { get; set; }

        [JsonProperty("conditions")]
        public List<FilterConditionDTO> Conditions { get; set; }
    }
}