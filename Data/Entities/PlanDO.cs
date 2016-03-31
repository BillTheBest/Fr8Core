﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.States.Templates;
using System.Linq;
using System;
using System.Reflection;
using Data.Infrastructure.StructureMap;
using StructureMap;
using Data.Interfaces;
using Data.States;

namespace Data.Entities
{
    public class PlanDO : PlanNodeDO
    {
		    
        private static readonly PropertyInfo[] TrackingProperties =
        {
            typeof(PlanDO).GetProperty("Name"),
            typeof(PlanDO).GetProperty("Tag"),
            typeof(PlanDO).GetProperty("Description"),
            typeof(PlanDO).GetProperty("PlanState"),
            typeof(PlanDO).GetProperty("Category"),
            typeof(PlanDO).GetProperty("Visibility")
        };
     
        public PlanDO()
        {
            Visibility = PlanVisibility.Standard;
        }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        /*[ForeignKey("StartingSubPlan")]
        public int StartingSubPlanId { get; set; }

        public virtual SubPlanDO StartingSubPlan { get; set; }*/

        [NotMapped]
        public Guid StartingSubPlanId
        {
            get
            {
                var startingSubPlan = ChildNodes.OfType<SubPlanDO>()
                    .SingleOrDefault(pnt => pnt.StartingSubPlan == true);
                if (null != startingSubPlan)
                {
                    return startingSubPlan.Id;
                }
                else
                {
                    return Guid.Empty;
                    //throw new ApplicationException("Starting SubPlan doesn't exist.");
                }
            }
        }

        [NotMapped]
        public SubPlanDO StartingSubPlan
        {
            get
            {
                return SubPlans.SingleOrDefault(pnt => pnt.StartingSubPlan == true);
            }

            set
            {
                var startingSubPlan = SubPlans.SingleOrDefault(pnt => pnt.StartingSubPlan == true);
                if (null != startingSubPlan)
                    startingSubPlan = value;
                else
                {
                    SubPlans.ToList().ForEach(pnt => pnt.StartingSubPlan = false);
                    if (value != null) 
                    { 
                        value.StartingSubPlan = true;
                        ChildNodes.Add(value);
                    }

                }
            }
        }

        [Required]
        [ForeignKey("PlanStateTemplate")]
        public int PlanState { get; set; }


        public virtual _PlanStateTemplate PlanStateTemplate { get; set; }

        public string Tag { get; set; }
        
        public PlanVisibility Visibility { get; set; }

        public string Category { get; set; }

        [NotMapped]
        public IEnumerable<SubPlanDO> SubPlans
        {
            get
            {
                return ChildNodes.OfType<SubPlanDO>();
            }
        }

        protected override IEnumerable<PropertyInfo> GetTrackingProperties()
        {
            foreach (var trackingProperty in base.GetTrackingProperties())
            {
                yield return trackingProperty;
            }

            foreach (var trackingProperty in TrackingProperties)
            {
                yield return trackingProperty;
            }
        }

        protected override PlanNodeDO CreateNewInstance()
        {
            return new PlanDO();
        }

        public override void AfterCreate()
        {
            base.AfterCreate();

            var securityService = ObjectFactory.GetInstance<ISecurityServices>();
            securityService.SetupDefaultSecurity(Id);
        }

        protected override void CopyProperties(PlanNodeDO source)
        {
            var plan = (PlanDO)source;

            base.CopyProperties(source);
            Name = plan.Name;
            Tag = plan.Tag;
            PlanState = plan.PlanState;
            Description = plan.Description;
            Visibility = plan.Visibility;
            Category = plan.Category;
            LastUpdated = plan.LastUpdated;
        }

        public bool IsOngoingPlan()
        {
            bool isOngoingPlan = false;
            var initialActivity = this.StartingSubPlan.ChildNodes.OrderBy(x => x.Ordering).FirstOrDefault() as ActivityDO;
            if (initialActivity != null)
            {
                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    var activityTemplate = uow.ActivityTemplateRepository.GetByKey(initialActivity.ActivityTemplateId);
                    if (activityTemplate.Category == ActivityCategory.Solution)
                    {
                        // Handle solutions
                        initialActivity = initialActivity.ChildNodes.OrderBy(x => x.Ordering).FirstOrDefault() as ActivityDO;
                        if (initialActivity != null)
                        {
                            activityTemplate = uow.ActivityTemplateRepository.GetByKey(initialActivity.ActivityTemplateId);
                        }
                        else
                        {
                            return isOngoingPlan;
                        }
                    }

                    if (activityTemplate != null && activityTemplate.Category == ActivityCategory.Monitors)
                    {
                        isOngoingPlan = true;
                    }
                }
            }
            return isOngoingPlan;
        }


    }
}