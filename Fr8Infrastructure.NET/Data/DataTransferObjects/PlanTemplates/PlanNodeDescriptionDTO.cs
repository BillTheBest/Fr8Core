﻿using System;
using System.Collections.Generic;

namespace fr8.Infrastructure.Data.DataTransferObjects.PlanTemplates
{
    public class PlanNodeDescriptionDTO
    {
        public Guid Id { get; set; }

        public Guid? ParentNodeId { get; set; }

        public string Name { get; set; }

        public List<NodeTransitionDTO> Transitions { get; set; }

        public ActivityDescriptionDTO ActivityDescription { get; set; }

        public string SubPlanName { get; set; }

        public string SubPlanOriginalId { get; set; }

        public bool IsStartingSubplan { get; set; }
    }
}
