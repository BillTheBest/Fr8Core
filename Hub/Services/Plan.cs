﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AutoMapper;
using Hub.Exceptions;
using Microsoft.AspNet.Identity.EntityFramework;
using StructureMap;
using Data.Entities;
using Data.Exceptions;
using Data.Infrastructure.StructureMap;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Interfaces;
using InternalInterface = Hub.Interfaces;
using Hub.Managers;
using System.Threading.Tasks;
using Data.Constants;
using Data.Crates;
using Data.Infrastructure;
using Data.Interfaces.DataTransferObjects.Helpers;
using Data.Repositories.Plan;
using Hub.Managers.APIManagers.Transmitters.Restful;

namespace Hub.Services
{
    public class Plan : IPlan
    {
        // private readonly IProcess _process;
        private readonly InternalInterface.IContainer _container;
        private readonly Fr8Account _dockyardAccount;
        private readonly IActivity _activity;
        private readonly ICrateManager _crate;
        private readonly ISecurityServices _security;
        private readonly IProcessNode _processNode;
        private readonly IJobDispatcher _dispatcher;

        public Plan(InternalInterface.IContainer container, Fr8Account dockyardAccount, IActivity activity,
            ICrateManager crate, ISecurityServices security, IProcessNode processNode, IJobDispatcher dispatcher)
        {
            _container = container;
            _dockyardAccount = dockyardAccount;
            _activity = activity;
            _crate = crate;
            _security = security;
            _processNode = processNode;
            _dispatcher = dispatcher;
        }

        public IList<PlanDO> GetForUser(IUnitOfWork unitOfWork, Fr8AccountDO account, bool isAdmin = false,
            Guid? id = null, int? status = null, string category = "")
        {
            var planQuery = unitOfWork.PlanRepository.GetPlanQueryUncached()
                .Where(x => x.Visibility == PlanVisibility.Standard);

            planQuery = (id == null
                ? planQuery.Where(pt => pt.Fr8Account.Id == account.Id)
                : planQuery.Where(pt => pt.Id == id && pt.Fr8Account.Id == account.Id));

            if (!string.IsNullOrEmpty(category))
                planQuery = planQuery.Where(c => c.Category == category);
            else
                planQuery = planQuery.Where(c => string.IsNullOrEmpty(c.Category));

            return (status == null
                ? planQuery.Where(pt => pt.PlanState != PlanState.Deleted)
                : planQuery.Where(pt => pt.PlanState == status)).ToList();

        }

        public IList<PlanDO> GetByName(IUnitOfWork uow, Fr8AccountDO account, string name, PlanVisibility visibility)
        {
            if (name != null)
            {
                return
                    uow.PlanRepository.GetPlanQueryUncached()
                        .Where(r => r.Fr8Account.Id == account.Id && r.Name == name)
                        .Where(p => p.PlanState != PlanState.Deleted && p.Visibility == visibility)
                        .ToList();
            }

            return
                uow.PlanRepository.GetPlanQueryUncached()
                    .Where(r => r.Fr8Account.Id == account.Id)
                    .Where(p => p.PlanState != PlanState.Deleted && p.Visibility == visibility)
                    .ToList();
        }

        public void CreateOrUpdate(IUnitOfWork uow, PlanDO submittedPlan, bool updateChildEntities)
        {
            if (submittedPlan.Id == Guid.Empty)
            {
                submittedPlan.Id = Guid.NewGuid();
                submittedPlan.PlanState = PlanState.Inactive;
                submittedPlan.Fr8Account = _security.GetCurrentAccount(uow);

                submittedPlan.ChildNodes.Add(new SubPlanDO(true)
                {
                    Id = Guid.NewGuid(),
                    Name = "Starting Subplan"
                });

                uow.PlanRepository.Add(submittedPlan);
            }
            else
            {
                var curPlan = uow.PlanRepository.GetById<PlanDO>(submittedPlan.Id);

                if (curPlan == null)
                {
                    throw new EntityNotFoundException();
                }

                curPlan.Name = submittedPlan.Name;
                curPlan.Description = submittedPlan.Description;
                curPlan.Category = submittedPlan.Category;
            }
        }

        public PlanDO Create(IUnitOfWork uow, string name, string category = "")
        {
            var plan = new PlanDO
            {
                Id = Guid.NewGuid(),
                Name = name,
                Fr8Account = _security.GetCurrentAccount(uow),
                PlanState = PlanState.Inactive,
                Category = category
            };

            uow.PlanRepository.Add(plan);

            return plan;
        }

        public void Delete(IUnitOfWork uow, Guid id)
        {
            var plan = uow.PlanRepository.GetById<PlanDO>(id);

            if (plan == null)
            {
                throw new EntityNotFoundException<PlanDO>(id);
            }

            plan.PlanState = PlanState.Deleted;

            //Plan deletion will only update its PlanState = Deleted
            foreach (var container in _container.LoadContainers(uow, plan))
            {
                container.ContainerState = ContainerState.Deleted;
            }
        }


        public async Task<ActivateActivitiesDTO> Activate(Guid curPlanId, bool planBuilderActivate)
        {
            var result = new ActivateActivitiesDTO
            {
                Status = "no action",
                ActivitiesCollections = new List<ActivityDTO>()
            };

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = uow.PlanRepository.GetById<PlanDO>(curPlanId);

                if (plan.PlanState == PlanState.Deleted)
                {
                    EventManager.PlanActivationFailed(plan, "Cannot activate deleted plan");
                    throw new ApplicationException("Cannot activate deleted plan");
                }

                foreach (var curActionDO in plan.GetDescendants().OfType<ActivityDO>())
                {
                    try
                    {
                        var resultActivate = await _activity.Activate(curActionDO);

                        ContainerDTO errorContainerDTO;
                        result.Status = "success";

                        var validationErrorChecker = CheckForExistingValidationErrors(resultActivate, out errorContainerDTO);
                        if (validationErrorChecker)
                        {
                            result.Status = "validation_error";
                            result.Container = errorContainerDTO;
                        }

                        //if the activate call is comming from the Plan Builder just render again the action group with the errors
                        if (planBuilderActivate)
                        {
                            result.ActivitiesCollections.Add(resultActivate);
                        }
                        else if (validationErrorChecker)
                        {
                            //if the activate call is comming from the Plans List then show the first error message and redirect to plan builder 
                            //so the user could fix the configuration
                            result.RedirectToPlanBuilder = true;

                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException(string.Format("Process template activation failed for action {0}.", curActionDO.Label), ex);
                    }
                }


                if (result.Status != "validation_error")
                {
                    plan.PlanState = PlanState.Active;
                    uow.SaveChanges();
                }
            }

            return result;
        }

        /// <summary>
        /// After receiving response from terminals for activate action call, checks for existing validation errors on some controls
        /// </summary>
        /// <param name="curActivityDTO"></param>
        /// <param name="containerDTO">Use containerDTO as a wrapper for the Error with proper ActivityResponse and error DTO</param>
        /// <returns></returns>
        private bool CheckForExistingValidationErrors(ActivityDTO curActivityDTO, out ContainerDTO containerDTO)
        {
            containerDTO = new ContainerDTO();

            var crateStorage = _crate.GetStorage(curActivityDTO);

            var configControls = crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().ToList();
            //search for an error inside the config controls and return back if exists
            foreach (var controlGroup in configControls)
            {
                var control = controlGroup.Controls.FirstOrDefault(x => !string.IsNullOrEmpty(x.ErrorMessage));
                if (control != null)
                {
                    var containerDO = new ContainerDO() { CrateStorage = string.Empty, Name = string.Empty };

                    using (var tempCrateStorage = _crate.UpdateStorage(() => containerDO.CrateStorage))
                    {
                        var operationalState = new OperationalStateCM();
                        operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.Error);
                        operationalState.CurrentActivityResponse.AddErrorDTO(ErrorDTO.Create(control.ErrorMessage, ErrorType.Generic, string.Empty, null, curActivityDTO.ActivityTemplate.Name, curActivityDTO.ActivityTemplate.Terminal.Name));

                        var operationsCrate = Crate.FromContent("Operational Status", operationalState);
                        tempCrateStorage.Add(operationsCrate);
                    }

                    containerDTO = Mapper.Map<ContainerDTO>(containerDO);
                    return true;
                }
            }

            return false;
        }

        public async Task<string> Deactivate(Guid curPlanId)
        {
            string result = "no action";

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = uow.PlanRepository.GetById<PlanDO>(curPlanId);


                foreach (var curActionDO in plan.GetDescendants().OfType<ActivityDO>())
                {
                    try
                    {
                        await _activity.Deactivate(curActionDO);
                        result = "success";
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException("Process template activation failed.", ex);
                    }
                }


                plan.PlanState = PlanState.Inactive;
                uow.SaveChanges();
            }

            return result;
        }


        public IList<PlanDO> GetMatchingPlans(string userId, EventReportCM curEventReport)
        {
            List<PlanDO> planSubscribers = new List<PlanDO>();
            if (String.IsNullOrEmpty(userId))
                throw new ArgumentNullException("Parameter UserId is null");
            if (curEventReport == null)
                throw new ArgumentNullException("Parameter Standard Event Report is null");

            //1. Query all PlanDO that are Active
            //2. are associated with the determined DockyardAccount
            //3. their first Activity has a Crate of  Class "Standard Event Subscriptions" which has inside of it an event name that matches the event name 
            //in the Crate of Class "Standard Event Reports" which was passed in.
            var curPlans = _dockyardAccount.GetActivePlans(userId).ToList();

            return MatchEvents(curPlans, curEventReport);
            //3. Get ActivityDO

        }


        public List<PlanDO> MatchEvents(List<PlanDO> curPlans, EventReportCM curEventReport)
        {
            List<PlanDO> subscribingPlans = new List<PlanDO>();

            foreach (var curPlan in curPlans)
            {
                //get the 1st activity
                var actionDO = GetFirstActivityWithEventSubscriptions(curPlan.Id);

                if (actionDO != null)
                {
                    var storage = _crate.GetStorage(actionDO.CrateStorage);

                    foreach (var subscriptionsList in storage.CrateContentsOfType<EventSubscriptionCM>())
                    {
                        var manufacturer = subscriptionsList.Manufacturer;
                        bool hasEvents;
                        if (string.IsNullOrEmpty(manufacturer) || string.IsNullOrEmpty(curEventReport.Manufacturer))
                        {
                            hasEvents = subscriptionsList.Subscriptions.Any(events => curEventReport.EventNames.ToUpper().Trim().Replace(" ", "").Contains(events.ToUpper().Trim().Replace(" ", "")));
                        }
                        else
                        {
                            hasEvents = subscriptionsList.Subscriptions.Any(events => curEventReport.Manufacturer == manufacturer &&
                                curEventReport.EventNames.ToUpper().Trim().Replace(" ", "").Contains(events.ToUpper().Trim().Replace(" ", "")));
                        }

                        if (hasEvents)
                        {
                            subscribingPlans.Add(curPlan);
                        }
                    }
                }
            }

            return subscribingPlans;
        }

        private ActivityDO GetFirstActivityWithEventSubscriptions(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var root = uow.PlanRepository.GetById<PlanDO>(id);
                if (root == null)
                {
                    return null;
                }

                return root.GetDescendantsOrdered().OfType<ActivityDO>().FirstOrDefault(
                    x =>
                    {
                        var storage = _crate.GetStorage(x.CrateStorage);
                        return storage.CratesOfType<EventSubscriptionCM>().Any();
                    });

            }
        }
        
        public PlanDO GetPlanByActivityId(IUnitOfWork uow, Guid id)
        {
            return (PlanDO)uow.PlanRepository.GetById<PlanNodeDO>(id).GetTreeRoot();
        }

        public PlanDO Copy(IUnitOfWork uow, PlanDO plan, string name)
        {
            throw new NotImplementedException();

            //            var root = (PlanDO)plan.Clone();
            //            root.Id = Guid.NewGuid();
            //            root.Name = name;
            //            uow.PlanRepository.Add(root);
            //
            //            var queue = new Queue<Tuple<PlanNodeDO, Guid>>();
            //            queue.Enqueue(new Tuple<PlanNodeDO, Guid>(root, plan.Id));
            //
            //            while (queue.Count > 0)
            //            {
            //                var planTuple = queue.Dequeue();
            //                var planNode = planTuple.Item1;
            //                var sourcePlanNodeId = planTuple.Item2;
            //
            //                var sourceChildren = uow.
            //                    .GetQuery()
            //                    .Where(x => x.ParentPlanNodeId == sourcePlanNodeId)
            //                    .ToList();
            //
            //                foreach (var sourceChild in sourceChildren)
            //                {
            //                    var childCopy = sourceChild.Clone();
            //                    childCopy.Id = Guid.NewGuid();
            //                    childCopy.ParentPlanNode = planNode;
            //                    planNode.ChildNodes.Add(childCopy);
            //
            //                    uow.PlanNodeRepository.Add(childCopy);
            //
            //                    queue.Enqueue(new Tuple<PlanNodeDO, Guid>(childCopy, sourceChild.Id));
            //                }
            //            }
            //
            //            return root;
        }

        /// <summary>
        /// New Process object
        /// </summary>
        /// <param name="planId"></param>
        /// <param name="envelopeId"></param>
        /// <returns></returns>
        public ContainerDO Create(IUnitOfWork uow, PlanDO curPlan, params Crate[] curPayload)
        {
            //let's exclude null payload crates
            curPayload = curPayload.Where(c => c != null).ToArray();

            var containerDO = new ContainerDO { Id = Guid.NewGuid() };

            containerDO.PlanId = curPlan.Id;
            containerDO.Name = curPlan.Name;
            containerDO.ContainerState = ContainerState.Unstarted;

                using (var crateStorage = _crate.UpdateStorage(() => containerDO.CrateStorage))
                {
                if (curPayload.Length > 0)
                {
                    crateStorage.AddRange(curPayload);
                    crateStorage.Remove<OperationalStateCM>();
                }
                
                var operationalState = new OperationalStateCM();
                
                operationalState.CallStack.Push(new OperationalStateCM.StackFrame
                {
                    NodeName = "Starting subplan",
                    NodeId = curPlan.StartingSubPlanId,
                });

                crateStorage.Add(Crate.FromContent("Operational state", operationalState));
            }

            uow.ContainerRepository.Add(containerDO);

            //then create process node
            var curProcessNode = _processNode.Create(uow, containerDO.Id, curPlan.StartingSubPlanId, "process node");
            containerDO.ProcessNodes.Add(curProcessNode);

            uow.SaveChanges();
            EventManager.ContainerCreated(containerDO);

            return containerDO;
        }

        private async Task<ContainerDO> Run(IUnitOfWork uow, ContainerDO curContainerDO)
        {
            var plan = uow.PlanRepository.GetById<PlanDO>(curContainerDO.PlanId);

            if (plan.PlanState == PlanState.Deleted)
            {
                throw new ApplicationException("Cannot run plan that is in deleted state.");
            }

            if (curContainerDO.ContainerState == ContainerState.Failed
                || curContainerDO.ContainerState == ContainerState.Completed)
            {
                throw new ApplicationException("Attempted to Launch a Process that was Failed or Completed");
            }

            try
            {
                await _container.Run(uow, curContainerDO);
                return curContainerDO;
            }
            catch (Exception)
            {
                curContainerDO.ContainerState = ContainerState.Failed;
                throw;
            }
            finally
            {
                //TODO is this necessary? let's leave it as it is
                /*
                curContainerDO.CurrentPlanNode = null;
                curContainerDO.NextPlanNode = null;
                 * */
                uow.SaveChanges();
            }
        }

        public async Task<ContainerDO> Run(IUnitOfWork uow, PlanDO curPlan, params Crate[] curPayload)
        {
            var curContainerDO = Create(uow, curPlan, curPayload);
            return await Run(uow, curContainerDO);
        }

        public void Enqueue(Guid curPlanId, params Crate[] curEventReport)
        {
            _dispatcher.Enqueue(() => LaunchProcess(curPlanId, curEventReport).Wait());
        }

        public void Enqueue(Guid curPlanId, Guid curSubPlanId, params Crate[] curEventReport)
        {
            _dispatcher.Enqueue(() => LaunchProcess(curPlanId, curSubPlanId, curEventReport).Wait());
        }

        public void Enqueue(List<PlanDO> curPlans, params Crate[] curEventReport)
        {
            foreach (var curPlan in curPlans)
            {
                Enqueue(curPlan.Id, curEventReport);
            }
        }

        public static async Task LaunchProcess(Guid curPlan, params Crate[] curPayload)
        {
            if (curPlan == default(Guid))
                throw new ArgumentException(nameof(curPlan));

            await ObjectFactory.GetInstance<IPlan>().Run(curPlan, curPayload);
        }

        public static async Task LaunchProcess(Guid curPlan, Guid curSubPlan, params Crate[] curPayload)
        {
            if (curPlan == default(Guid))
                throw new ArgumentException(nameof(curPlan));
            if (curSubPlan == default(Guid))
                throw new ArgumentException(nameof(curSubPlan));

            await ObjectFactory.GetInstance<IPlan>().Run(curPlan, curSubPlan, curPayload);
        }

        public async Task<ContainerDO> Run(Guid planId, params Crate[] curPayload)
        {
            return await Run(planId, default(Guid), curPayload);
        }

        public async Task<ContainerDO> Run(Guid planId, Guid subPlanId, params Crate[] curPayload)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curPlan = uow.PlanRepository.GetById<PlanDO>(planId);
                if (curPlan == null)
                    throw new ArgumentNullException("planId");
                try
                {
                    var curContainerDO = Create(uow, curPlan, curPayload);
                    if (subPlanId != default(Guid))
                    {
                        curContainerDO.CurrentPlanNodeId = GetFirstActivityOfSubplan(uow, curContainerDO, subPlanId);
                    }
                return await Run(uow, curContainerDO);
            }
                catch (Exception ex)
                {
                    EventManager.ContainerFailed(curPlan, ex);
                    throw;
                }
        }
        }

        public async Task<ContainerDO> Run(PlanDO curPlan, params Crate[] curPayload)
        {
            return await Run(curPlan.Id, curPayload);
        }

        public async Task<ContainerDO> Continue(Guid containerId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curContainerDO = uow.ContainerRepository.GetByKey(containerId);
                if (curContainerDO.ContainerState != ContainerState.Pending)
                {
                    throw new ApplicationException("Attempted to Continue a Process that wasn't pending");
                }
                //continue from where we left
                return await Run(uow, curContainerDO);
            }
        }

        public async Task<PlanDO> Clone(Guid planId)
        {
            
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var currentUser = _security.GetCurrentAccount(uow);

                var targetPlan = (PlanDO)GetPlanByActivityId(uow, planId);
                if (targetPlan == null)
                {
                    return null;
                }

                var cloneTag = "Cloned From " + planId;
                //let's check if we have cloned this plan before
                var existingPlan = await uow.PlanRepository.GetPlanQueryUncached()
                    .Where(p => p.Fr8AccountId == currentUser.Id && p.Tag.Contains(cloneTag)).FirstOrDefaultAsync();


                if (existingPlan != null)
                {
                    //we already have cloned this plan before
                    return existingPlan;
                }

                //we should clone this plan for current user
                //let's clone the plan entirely
                var clonedPlan = (PlanDO)PlanTreeHelper.CloneWithStructure(targetPlan);
                clonedPlan.Name = clonedPlan.Name + " - " + "Customized for User " + currentUser.UserName;
                clonedPlan.PlanState = PlanState.Inactive;
                clonedPlan.Tag = cloneTag;
                
                //linearlize tree structure
                var planTree = clonedPlan.GetDescendantsOrdered();
                

                //let's replace old id's of cloned plan with new id's
                //and update account information
                //TODO maybe we should do something about authorization tokens too?
                Dictionary<Guid, PlanNodeDO> parentMap = new Dictionary<Guid, PlanNodeDO>();
                foreach (var planNodeDO in planTree)
                {
                    var oldId = planNodeDO.Id;
                    planNodeDO.Id = Guid.NewGuid();
                    planNodeDO.Fr8Account = currentUser;
                    parentMap.Add(oldId, planNodeDO);
                    planNodeDO.ChildNodes = new List<PlanNodeDO>();
                    if (planNodeDO.ParentPlanNodeId != null)
                    {
                        PlanNodeDO newParent;
                        //find parent from old parent id map
                        if (parentMap.TryGetValue(planNodeDO.ParentPlanNodeId.Value, out newParent))
                        {
                            //replace parent id with parent's new id
                            planNodeDO.ParentPlanNodeId = newParent.Id;
                            newParent.ChildNodes.Add(planNodeDO);
                        }
                        else
                        {
                            //this should never happen
                            throw new Exception("Unable to clone plan");
                        }
                    }
                    else
                    {
                        //this should be a plan because it has null ParentId
                        uow.PlanRepository.Add(planNodeDO as PlanDO);
                    }
                }
                

                //save new cloned plan
                uow.SaveChanges();

                return clonedPlan;
            }
        }

        public ContainerDO Create(IUnitOfWork uow, Guid planId, params Crate[] curPayload)
        {
            var curPlan = uow.PlanRepository.GetById<PlanDO>(planId);
            if (curPlan == null)
                throw new ArgumentNullException("planId");
            return Create(uow, curPlan, curPayload);
        }

        private Guid? GetFirstActivityOfSubplan(IUnitOfWork uow, ContainerDO curContainerDO, Guid subplanId)
        {
            var subplan = uow.PlanRepository.GetById<PlanDO>(curContainerDO.PlanId).SubPlans.FirstOrDefault(s => s.Id == subplanId);
            return subplan?.ChildNodes.OrderBy(c => c.Ordering).FirstOrDefault()?.Id;
        }
    }
}