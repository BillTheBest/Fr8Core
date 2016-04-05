﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data.Constants;
using Data.Crates;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.DataTransferObjects.Helpers;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Exceptions;
using Hub.Interfaces;
using Hub.Managers;
using StructureMap;

namespace Hub.Services
{
    partial class Container
    {
        // class handling execution of the particular plan
        private class ExecutionSession
        {
            /**********************************************************************************/
            // Declarations
            /**********************************************************************************/
#if DEBUG
            private const int MaxStackSize = 100;
#else
            private const int MaxStackSize = 250;
#endif

            private readonly IUnitOfWork _uow;
            private readonly OperationalStateCM.ActivityCallStack _callStack;
            private readonly ContainerDO _container;
            private OperationalStateCM _operationalState;
            private readonly IActivity _activity;
            private readonly ICrateManager _crate;
            
            /**********************************************************************************/
            // Functions
            /**********************************************************************************/

            public ExecutionSession(IUnitOfWork uow, OperationalStateCM.ActivityCallStack callStack, ContainerDO container, IActivity activity, ICrateManager crateManager)
            {
                _uow = uow;
                _callStack = callStack;
                _container = container;

                _activity = activity;
                _crate = crateManager;
            }

            /**********************************************************************************/

            private OperationalStateCM GetOperationalState(ICrateStorage crateStorage)
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().FirstOrDefault();

                if (operationalState == null)
                {
                    throw new Exception("OperationalState was not found within the container payload.");
                }

                return operationalState;
            }

            /**********************************************************************************/

            private void PushFrame(Guid nodeId)
            {
                var node = _uow.PlanRepository.GetById<PlanNodeDO>(nodeId);
                string nodeName = "undefined";

                if (node is ActivityDO)
                {
                    nodeName = "Activity: " + ((ActivityDO) node).Label;
                }

                if (node is SubPlanDO)
                {
                    nodeName = "Subplan: " + ((SubPlanDO) node).Name;
                }

                var frame = new OperationalStateCM.StackFrame
                {
                    NodeId = nodeId,
                    NodeName = nodeName,
                    LocalData = _operationalState.BypassData
                };

                _callStack.Push(frame);
                _operationalState.BypassData = null;
            }

            /**********************************************************************************/
            // See https://maginot.atlassian.net/wiki/display/DDW/New+container+execution+logic for details
            public async Task Run()
            {
                while (_callStack.Count > 0)
                {
                    if (_callStack.Count > MaxStackSize)
                    {
                        throw new Exception("Container execution stack overflow");
                    }

                    var topFrame = _callStack.Peek();
                    var currentNode = _uow.PlanRepository.GetById<PlanNodeDO>(topFrame.NodeId);

                    try
                    {
                        try
                        {
                            using (var payloadStorage = _crate.UpdateStorage(() => _container.CrateStorage))
                            {
                                _operationalState = GetOperationalState(payloadStorage);

                                _operationalState.CallStack = _callStack;
                                // reset current activity response
                                _operationalState.CurrentActivityResponse = null;
                                // update container's payload
                                payloadStorage.Flush();

                                if (topFrame.CurrentActivityExecutionPhase == OperationalStateCM.ActivityExecutionPhase.WasNotExecuted)
                                {
                                    await ExecuteNode(currentNode, payloadStorage, ActivityExecutionMode.InitialRun);

                                    topFrame.CurrentActivityExecutionPhase = OperationalStateCM.ActivityExecutionPhase.ProcessingChildren;

                                    // process op codes
                                    if (!ProcessOpCodes(_operationalState.CurrentActivityResponse, OperationalStateCM.ActivityExecutionPhase.WasNotExecuted, topFrame))
                                    {
                                        break;
                                    }

                                    continue;
                                }

                                var currentChild = topFrame.CurrentChildId != null ? _uow.PlanRepository.GetById<PlanNodeDO>(topFrame.CurrentChildId.Value) : null;
                                var nextChild = currentChild != null ? currentNode.ChildNodes.OrderBy(x => x.Ordering).FirstOrDefault(x => x.Ordering > currentChild.Ordering) : currentNode.ChildNodes.OrderBy(x => x.Ordering).FirstOrDefault();

                                // if there is a child that has not being executed yet - mark it for execution by pushing to stack
                                if (nextChild != null)
                                {
                                    PushFrame(nextChild.Id);
                                    topFrame.CurrentChildId = nextChild.Id;
                                }
                                // or run current activity in ReturnFromChildren mode
                                else
                                {
                                    if (currentNode.ChildNodes.Count > 0)
                                    {
                                        await ExecuteNode(currentNode, payloadStorage, ActivityExecutionMode.ReturnFromChildren);
                                    }

                                    _callStack.Pop();

                                    // process op codes
                                    if (!ProcessOpCodes(_operationalState.CurrentActivityResponse, OperationalStateCM.ActivityExecutionPhase.ProcessingChildren, topFrame))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            _uow.SaveChanges();
                        }
                    }
                    catch (ErrorResponseException e)
                    {
                        throw new ActivityExecutionException(e.ContainerDTO, Mapper.Map<ActivityDO, ActivityDTO>((ActivityDO) currentNode), e.Message, e);
                    }
                    catch (Exception e)
                    {
                        var curActivity = currentNode as ActivityDO;

                        if (curActivity != null)
                        {
                            throw new ActivityExecutionException(Mapper.Map<ContainerDO, ContainerDTO>(_container), Mapper.Map<ActivityDO, ActivityDTO>(curActivity), string.Empty, e);
                        }

                        throw;
                    }
                }

                if (_container.ContainerState == ContainerState.Executing)
                {
                    _container.ContainerState = ContainerState.Completed;
                    _uow.SaveChanges();
                }
            }

            /**********************************************************************************/

            private Guid ExtractGuidParam(ActivityResponseDTO activityResponse)
            {
                ResponseMessageDTO responseMessage;

                if (!activityResponse.TryParseResponseMessageDTO(out responseMessage))
                {
                    throw new InvalidOperationException("Unable to parse op code parameter");
                }

                return  Guid.Parse((string)responseMessage.Details);
            }

            /**********************************************************************************/

            private bool ProcessOpCodes(ActivityResponseDTO activityResponse, OperationalStateCM.ActivityExecutionPhase activityExecutionPhase, OperationalStateCM.StackFrame topFrame)
            {
                ActivityResponse opCode;

                if (activityResponse == null)
                {
                    return true;
                }

                if (!Enum.TryParse(activityResponse.Type, out opCode))
                {
                    return true;
                }

                PlanNodeDO currentNode;
                PlanNodeDO targetNode;
                Guid id;

                switch (opCode)
                {
                    case ActivityResponse.Error:
                        ErrorDTO error = activityResponse.TryParseErrorDTO(out error) ? error : null;
                        throw new ErrorResponseException(Mapper.Map<ContainerDO, ContainerDTO>(_container), error?.Message);

                    case ActivityResponse.ExecuteClientActivity:
                        break;

                    case ActivityResponse.ShowDocumentation:
                        break;

                    case ActivityResponse.LaunchAdditionalPlan:
                        LoadAndRunPlan(ExtractGuidParam(activityResponse));
                        break;

                    case ActivityResponse.RequestTerminate:
                        _callStack.Clear();
                        EventManager.ProcessingTerminatedPerActivityResponse(_container, ActivityResponse.RequestTerminate);
                        return false;

                    case ActivityResponse.RequestSuspend:
                        _container.ContainerState = ContainerState.Pending;
                        
                        if (activityExecutionPhase == OperationalStateCM.ActivityExecutionPhase.ProcessingChildren)
                        {
                            _callStack.Push(topFrame);
                        }
                        else
                        {
                            // reset state of currently executed activity
                            topFrame.CurrentActivityExecutionPhase = OperationalStateCM.ActivityExecutionPhase.WasNotExecuted;
                        }

                        return false;

                    case ActivityResponse.SkipChildren:
                        if (activityExecutionPhase == OperationalStateCM.ActivityExecutionPhase.WasNotExecuted)
                        {
                            _callStack.Pop();
                        }
                        break;
                 
                    case ActivityResponse.JumpToSubplan:
                        id = ExtractGuidParam(activityResponse);
                        targetNode = _uow.PlanRepository.GetById<PlanNodeDO>(id);

                        if (targetNode == null)
                        {
                            throw new InvalidOperationException($"Unable to find node {id}");
                        }

                        currentNode = _uow.PlanRepository.GetById<PlanNodeDO>(topFrame.NodeId);

                        if (currentNode.RootPlanNodeId != targetNode.RootPlanNodeId)
                        {
                            throw new InvalidOperationException("Can't jump to the activity from different plan.");
                        }

                        _callStack.Clear();
                        PushFrame(id);
                        break;

                    case ActivityResponse.Jump:
                    case ActivityResponse.JumpToActivity:
                        id = ExtractGuidParam(activityResponse);
                        targetNode = _uow.PlanRepository.GetById<PlanNodeDO>(id);

                        if (targetNode == null)
                        {
                            throw new InvalidOperationException($"Unable to find node {id}");
                        }

                        currentNode = _uow.PlanRepository.GetById<PlanNodeDO>(topFrame.NodeId);

                        if (currentNode.RootPlanNodeId != targetNode.RootPlanNodeId)
                        {
                            throw new InvalidOperationException("Can't jump to the activity from different plan.");
                        }

                        if (targetNode.ParentPlanNodeId == null && currentNode.ParentPlanNodeId == null && currentNode.Id != targetNode.Id)
                        {
                            throw new InvalidOperationException("Can't jump from the activities that has no parent to anywhere except the activity itself.");
                        }

                        if (targetNode.ParentPlanNodeId != currentNode.ParentPlanNodeId)
                        {
                            throw new InvalidOperationException("Can't jump to activity that has parent different from activity we are jumping from.");
                        }
                        
                        // we are jumping after activity's Run
                        if (activityExecutionPhase == OperationalStateCM.ActivityExecutionPhase.WasNotExecuted)
                        {
                            // remove current activity from stack. 
                            _callStack.Pop();
                        }

                        if (id == topFrame.NodeId)
                        {
                            // we want to pass current local data (from the topFrame) to the next activity we are calling.
                            _operationalState.BypassData = topFrame.LocalData;
                        }

                        // this is root node. Just push new frame
                        if (_callStack.Count == 0 || currentNode.ParentPlanNode == null)
                        {
                            PushFrame(id);
                        }
                        else
                        {
                            var parentFrame = _callStack.Peek();
                            // find activity that is preceeding the one we are jumping to.
                            // so the next iteration of run cycle will exectute the activity we are jumping to
                            var prevToJump = currentNode.ParentPlanNode.ChildNodes.OrderByDescending(x => x.Ordering).FirstOrDefault(x => x.Ordering < targetNode.Ordering);

                            parentFrame.CurrentChildId = prevToJump?.Id;
                        }

                        break;

                    case ActivityResponse.Call:
                        id = ExtractGuidParam(activityResponse);

                        targetNode = _uow.PlanRepository.GetById<PlanNodeDO>(id);

                        if (targetNode == null)
                        {
                            throw new InvalidOperationException($"Unable to find node {id}");
                        }

                        currentNode = _uow.PlanRepository.GetById<PlanNodeDO>(topFrame.NodeId);

                        if (currentNode.RootPlanNodeId != targetNode.RootPlanNodeId)
                        {
                            throw new InvalidOperationException("Can't call the activity from different plan.");
                        }

                        PushFrame(id);
                        break;

                    case ActivityResponse.Break:
                        if (activityExecutionPhase == OperationalStateCM.ActivityExecutionPhase.WasNotExecuted)
                        {
                            _callStack.Pop();
                        }

                        if (_callStack.Count > 0)
                        {
                            _callStack.Pop();
                        }
                        break;
                }

                return true;
            }

            /**********************************************************************************/

            private void LoadAndRunPlan(Guid planId)
            {
                var plan = ObjectFactory.GetInstance<IPlan>();

                var planDO = _uow.PlanRepository.GetById<PlanDO>(planId);

                if (planDO == null)
                {
                    throw  new InvalidOperationException($"Plan {planId} was not found");
                }

                var crateStorage = _crate.GetStorage(_container.CrateStorage);
                var operationStateCrate = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();

                operationStateCrate.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.Null);

                operationStateCrate.History.Add(new OperationalStateCM.HistoryElement
                {
                    Description = "Launch Triggered by Container ID " + _container.Id
                });
                
                crateStorage.Remove<OperationalStateCM>();

                var payloadCrates = crateStorage.AsEnumerable().ToArray();

                plan.Enqueue(planDO.Id, payloadCrates);
            }

            /**********************************************************************************/
            // Executes node is passed if it is an activity
            private async Task ExecuteNode(PlanNodeDO currentNode, IUpdatableCrateStorage payloadStorage, ActivityExecutionMode mode)
            {
                var currentActivity = currentNode as ActivityDO;

                if (currentActivity == null)
                {
                    return;
                }

                var payload = await _activity.Run(_uow, currentActivity, mode, _container);

                if (payload != null)
                {
                    var activityPayloadStroage = _crate.FromDto(payload.CrateStorage);

                    SyncPayload(activityPayloadStroage, payloadStorage);
                }
            }

            /**********************************************************************************/
            //this method is for copying payload that activity returns with container's payload.
            private void SyncPayload(ICrateStorage activityPayloadStorage, IUpdatableCrateStorage containerStorage)
            {
                if (activityPayloadStorage == null)
                {
                    return;
                }

                containerStorage.Replace(activityPayloadStorage);

                _operationalState = GetOperationalState(containerStorage);

                // just replace call stack with what we are using while running container. Activity can't change call stack and even if it happens we want to discard such action
                // the only exception is changes to LocalData related to the top activity in the stack. Activity can change this data and we want to sync it.
                var localData = _operationalState.CallStack.Count > 0 ? _operationalState.CallStack.Peek().LocalData : null;
                _operationalState.CallStack = _callStack;

                if (_callStack.Count > 0)
                {
                    _callStack.Peek().LocalData = localData;
                }
            }

            /**********************************************************************************/
        }
    }
}
