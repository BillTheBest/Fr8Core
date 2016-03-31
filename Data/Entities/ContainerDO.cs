﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Interfaces.DataTransferObjects;
using Data.States.Templates;
using Data.Validations;
using FluentValidation;
using Newtonsoft.Json;
using System;
using Data.Infrastructure;
using System.Data.Entity.Infrastructure;


namespace Data.Entities
{
    public class ContainerDO : BaseObject
    {
        public ContainerDO()
        {
            ProcessNodes = new List<ProcessNodeDO>();
        }

        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; }

        //public string Fr8AccountId { get; set; }

        [Required]
        [ForeignKey("Plan")]
        public Guid PlanId { get; set; }
        public virtual PlanDO Plan { get; set; }

        public virtual ICollection<ProcessNodeDO> ProcessNodes { get; set; }

        [Required]
        [ForeignKey("ContainerStateTemplate")]
        public int ContainerState { get; set; }

        public virtual _ContainerStateTemplate ContainerStateTemplate { get; set; }
       
        public string CrateStorage { get; set; }

        public override void BeforeSave()
        {
            base.BeforeSave();

            ContainerValidator curValidator = new ContainerValidator();
            curValidator.ValidateAndThrow(this);

        }
        public override void OnModify(DbPropertyValues originalValues, DbPropertyValues currentValues)
        {
            base.OnModify(originalValues, currentValues);
        }
    }
}