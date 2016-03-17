using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StructureMap;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Interfaces;
using Hub.Managers;
using Utilities.Configuration.Azure;
using TerminalBase.Infrastructure;
using AutoMapper;
using Data.Helpers;
using Data.Interfaces.DataTransferObjects.Helpers;
using Hub.Helper;

namespace TerminalBase.BaseClasses
{
    //this method allows a specific Action to inject its own evaluation function into the 
    //standard ProcessConfigurationRequest
    public delegate ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO);

    public class BaseTerminalActivity
    {
        #region Fields

        protected ICrateManager CrateManager { get; private set; }
        protected static readonly string ConfigurationControlsLabel = "Configuration_Controls";
        public string CurrentFr8UserId { get; set; }
        protected string ActivityName { get; set; }

        private List<ActivityTemplateDTO> _activityTemplateCache = null;

        public IHubCommunicator HubCommunicator { get; set; }
        #endregion

        public static readonly HashSet<CrateManifestType>  ExcludedManifestTypes = new HashSet<CrateManifestType>()
        {
            ManifestDiscovery.Default.GetManifestType<StandardConfigurationControlsCM>(),
            ManifestDiscovery.Default.GetManifestType<EventSubscriptionCM>()
        };

        public BaseTerminalActivity() : this("Unknown")
        {

        }

        public BaseTerminalActivity(string actionName)
        {
            CrateManager = ObjectFactory.GetInstance<ICrateManager>();
            HubCommunicator = ObjectFactory.GetInstance<IHubCommunicator>();
            ActivityName = actionName;
        }

        public void SetCurrentUser(string userId)
        {
            CurrentFr8UserId = userId;
        }

        /// <summary>
        /// Creates a suspend request for hub execution
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO SuspendHubExecution(PayloadDTO payload)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.RequestSuspend);
            }

            return payload;
        }

        /// <summary>
        /// Creates a terminate request for hub execution
        /// TODO: we could include a reason message with this request
        /// after that we could stop throwing exceptions on actions
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO TerminateHubExecution(PayloadDTO payload, string message = null)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.RequestTerminate);
                operationalState.CurrentActivityResponse.AddResponseMessageDTO(new ResponseMessageDTO() { Message = message });
            }

            return payload;
        }

        /// <summary>
        /// Jumps to an activity that resides in same subplan as current activity
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO JumpToActivity(PayloadDTO payload, Guid targetActivityId)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.JumpToActivity);
                operationalState.CurrentActivityResponse.AddResponseMessageDTO(new ResponseMessageDTO() { Details = targetActivityId });
            }

            return payload;
        }

        /// <summary>
        /// Jumps to an activity that resides in same subplan as current activity
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO JumpToSubplan(PayloadDTO payload, Guid targetSubplanId)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.JumpToSubplan);
                operationalState.CurrentActivityResponse.AddResponseMessageDTO(new ResponseMessageDTO() { Details = targetSubplanId });
            }

            return payload;
        }

        /// <summary>
        /// returns success to hub
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO Success(PayloadDTO payload, string message = "")
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.Success); 
                operationalState.CurrentActivityResponse.AddResponseMessageDTO(new ResponseMessageDTO() { Message = message });
            }

            return payload;
        }
        
        protected PayloadDTO ExecuteClientActivity(PayloadDTO payload, string clientActionName)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.ExecuteClientActivity);
                operationalState.CurrentClientActivityName = clientActionName;
            }

            return payload;
        }

        /// <summary>
        /// skips children of this action
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO SkipChildren(PayloadDTO payload)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.SkipChildren);
            }

            return payload;
        }

        /// <summary>
        /// returns error to hub
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="errorMessage"></param>
        /// <param name="errorCode"></param>
        /// <param name="currentActivity">Activity where the error occured</param>
        /// <param name="currentTerminal">Terminal where the error occured</param>
        /// <returns></returns>
        protected PayloadDTO Error(PayloadDTO payload, string errorMessage = null, ActivityErrorCode? errorCode = null, string currentActivity = null, string currentTerminal = null)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityErrorCode = errorCode;
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.Error);
                operationalState.CurrentActivityResponse.AddErrorDTO(ErrorDTO.Create(errorMessage, ErrorType.Generic, errorCode.ToString(), null, currentActivity, currentTerminal));
            }

            return payload;
        }

        /// <summary>
        /// returns Needs authentication error to hub
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO NeedsAuthenticationError(PayloadDTO payload)
        {
            return Error(payload, "No AuthToken provided.", ActivityErrorCode.NO_AUTH_TOKEN_PROVIDED);
        }

        /// <summary>
        /// Creates a reprocess child actions request
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected PayloadDTO ReProcessChildActivities(PayloadDTO payload)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                var operationalState = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
                operationalState.CurrentActivityResponse = ActivityResponseDTO.Create(ActivityResponse.ReProcessChildren);
            }

            return payload;
        }
        
        public virtual async Task<PayloadDTO> ExecuteChildActivities(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            return Success(await GetPayload(curActivityDO, containerId));
        }

        protected void CheckAuthentication(AuthorizationTokenDO authTokenDO)
        {
            if (NeedsAuthentication(authTokenDO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }
        }

        protected bool NeedsAuthentication(AuthorizationTokenDO authTokenDO)
        {
            return authTokenDO == null || string.IsNullOrEmpty(authTokenDO.Token);
        }

        protected async Task<PayloadDTO> GetPayload(ActivityDO activityDO, Guid containerId)
        {
            return await HubCommunicator.GetPayload(activityDO, containerId, CurrentFr8UserId);
        }
        protected async Task<UserDTO> GetCurrentUserData(ActivityDO activityDO, Guid containerId)
        {
            return await HubCommunicator.GetCurrentUser(activityDO, containerId, CurrentFr8UserId);
        }

        protected async Task<RouteFullDTO> GetPlansByActivity(string activityId)
        {
            return await HubCommunicator.GetPlansByActivity(activityId, CurrentFr8UserId);
        }

        protected async Task<RouteFullDTO> UpdatePlan(RouteEmptyDTO plan)
        {
            return await HubCommunicator.UpdatePlan(plan, CurrentFr8UserId);
        }

        protected async Task<Crate> ValidateFields(List<FieldValidationDTO> requiredFieldList)
        {
            var result = await HubCommunicator.ValidateFields(requiredFieldList, CurrentFr8UserId);

            var validationErrorList = new List<FieldDTO>();
            //lets create necessary validationError crates
            for (var i = 0; i < result.Count; i++)
            {
                var fieldCheckResult = result[i];
                if (fieldCheckResult == FieldValidationResult.NotExists)
                {
                    validationErrorList.Add(new FieldDTO() { Key = requiredFieldList[i].FieldName, Value = "Required" });
                }
            }

            if (validationErrorList.Any())
            {
                return CrateManager.CreateDesignTimeFieldsCrate("Validation Errors", validationErrorList.ToArray());
            }

            return null;
        }

        protected async Task<CrateDTO> ValidateByStandartDesignTimeFields(ActivityDO curActivityDO, FieldDescriptionsCM designTimeFields)
        {
            var fields = designTimeFields.Fields;
            var validationList = fields.Select(f => new FieldValidationDTO(curActivityDO.Id, f.Key)).ToList();
            return CrateManager.ToDto(await ValidateFields(validationList));
        }

        //if the Action doesn't provide a specific method to override this, we just return null = no validation errors
        protected virtual async Task<ICrateStorage> ValidateActivity(ActivityDO curActivityDO)
        {
            return await Task.FromResult<ICrateStorage>(null);
        }

        protected async Task<ActivityDO> ProcessConfigurationRequest(ActivityDO curActivityDO, ConfigurationEvaluator configurationEvaluationResult, AuthorizationTokenDO authToken)
        {
            var configRequestType = configurationEvaluationResult(curActivityDO);
            if (configRequestType == ConfigurationRequestType.Initial)
            {
                return await InitialConfigurationResponse(curActivityDO, authToken);
            }

            else if (configRequestType == ConfigurationRequestType.Followup)
            {
                var validationErrors = await ValidateActivity(curActivityDO);
                if (validationErrors != null)
                {
                    using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
                    {
                        crateStorage.AddRange(validationErrors);
                    }
                    return curActivityDO;
                }
                return await FollowupConfigurationResponse(curActivityDO, authToken);
            }

            throw new InvalidDataException("Activity's Configuration Store does not contain connection_string field.");
        }

        /// <summary>
        /// Configure infrastructure.
        /// </summary>
        public virtual async Task<ActivityDO> Configure(ActivityDO activityDO, AuthorizationTokenDO authTokenDO)
        {
            return await ProcessConfigurationRequest(activityDO, ConfigurationEvaluator, authTokenDO);
        }

        /// <summary>
        /// This method "evaluates" as to what configuration should be called. 
        /// Every terminal action will have its own decision making; hence this method must be implemented in the relevant child class.
        /// </summary>
        /// <param name="curActivityDO"></param>
        /// <returns></returns>
        public virtual ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            throw new NotImplementedException("ConfigurationEvaluator method not implemented in child class.");
        }

        //if the Action doesn't provide a specific method to override this, we just return the existing CrateStorage, unchanged
        protected virtual async Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            //Returns Task<ActivityDTO> using FromResult as the return type is known
            return await Task.FromResult<ActivityDO>(curActivityDO);
        }

        public virtual async Task<ActivityDO> Activate(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            //Returns Task<ActivityDTO> using FromResult as the return type is known
            var validationErrors = await ValidateActivity(curActivityDO);
            if (validationErrors != null)
            {
                using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
                {
                    crateStorage.AddRange(validationErrors);
                }
                return curActivityDO;
            }
            return await Task.FromResult<ActivityDO>(curActivityDO);
        }

        public virtual async Task<ActivityDO> Deactivate(ActivityDO curActivityDO)
        {
            //Returns Task<ActivityDTO> using FromResult as the return type is known
            return await Task.FromResult<ActivityDO>(curActivityDO);
        }

        //if the Action doesn't provide a specific method to override this, we just return the existing CrateStorage, unchanged
        protected virtual async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            //Returns Task<ActivityDTO> using FromResult as the return type is known
            return await Task.FromResult<ActivityDO>(curActivityDO);
        }

      
        
        protected void UpdateDesignTimeCrateValue(ICrateStorage storage, string label, params FieldDTO[] fields)
        {
            var crate = storage.CratesOfType<FieldDescriptionsCM>().FirstOrDefault(x => x.Label == label);

            if (crate == null)
            {
                crate = CrateManager.CreateDesignTimeFieldsCrate(label, fields);

                storage.Add(crate);
            }
            else
            {
                crate.Content.Fields.Clear();
                crate.Content.Fields.AddRange(fields);
            }
        }
        
        protected async Task<ActivityDO> AddAndConfigureChildActivity(Guid parentActivityId, string templateName_Or_templateID, string name = null, string label = null, int? order = null)
        {
            //search activity template by name or id
            var allActivityTemplates = _activityTemplateCache ?? (_activityTemplateCache = await HubCommunicator.GetActivityTemplates(CurrentFr8UserId));
            int templateId;
            var activityTemplate = Int32.TryParse(templateName_Or_templateID, out templateId) ?
                allActivityTemplates.FirstOrDefault(a => a.Id == templateId)
                : allActivityTemplates.FirstOrDefault(a => a.Name == templateName_Or_templateID);

            if (activityTemplate == null) throw new Exception(string.Format("ActivityTemplate {0} was not found", templateName_Or_templateID));

            //assign missing properties
            label = string.IsNullOrEmpty(label) ? activityTemplate.Label : label;
            name = string.IsNullOrEmpty(name) ? activityTemplate.Label : label;

            //parent must be a Subroute
            //If Route is specified as a parent, then a new subroute will be created
            //Guid parentId = (parent.ChildNodes.Count > 0) ? parent.ChildNodes[0].ParentRouteNodeId.Value : parent.RootRouteNodeId.Value;

            var result = await HubCommunicator.CreateAndConfigureActivity(activityTemplate.Id, CurrentFr8UserId, label, order, parentActivityId);
            var resultDO = Mapper.Map<ActivityDO>(result);
            return resultDO;
        }

        protected async Task<ActivityDO> AddAndConfigureChildActivity(ActivityDO parent, string templateName_Or_templateID, string name = null, string label = null, int? order = null)
        {

            var resultDO = await AddAndConfigureChildActivity(parent.Id, templateName_Or_templateID, name, label, order);

            if (resultDO != null)
            {
                parent.ChildNodes.Add(resultDO);
                return resultDO;
            }

            return null;
        }


        protected async Task<ActivityDO> ConfigureChildActivity(ActivityDO parent, ActivityDO child)
        {
            var result = await HubCommunicator.ConfigureActivity(child, CurrentFr8UserId);
            parent.ChildNodes.Remove(child);
            parent.ChildNodes.Add(result);
            return result;
        }


        /// <summary>
        /// Update Plan name if the current Plan name is the same as the passed parameter OriginalPlanName to avoid overwriting the changes made by the user
        /// </summary>
        /// <param name="activityDO"></param>
        /// <param name="OriginalPlanName"></param>
        /// <returns></returns>
        public async Task<RouteFullDTO> UpdatePlanName(Guid activityId, string OriginalPlanName, string NewPlanName)
        {
            try
            {
                RouteFullDTO plan = await GetPlansByActivity(activityId.ToString());
                if (plan != null && plan.Name.Equals(OriginalPlanName, StringComparison.OrdinalIgnoreCase))
                {
                    plan.Name = NewPlanName;

                    var emptyPlanDTO = Mapper.Map<RouteEmptyDTO>(plan);
                    plan = await UpdatePlan(emptyPlanDTO);
                }

                return plan;

            }
            catch (Exception ex)
            {
            }
            return null;
        }

        public async Task<RouteFullDTO> UpdatePlanCategory(Guid activityId, string category)
        {
            RouteFullDTO plan = await GetPlansByActivity(activityId.ToString());
            if (plan != null)
            {
                plan.Category = category;

                var emptyPlanDTO = Mapper.Map<RouteEmptyDTO>(plan);
                plan = await UpdatePlan(emptyPlanDTO);
            }

            return plan;
        }

       
        public ActivityResponseDTO GenerateDocumentationRepsonse(string documentation)
        {
            return new ActivityResponseDTO
            {
                Body = documentation,
                Type = ActivityResponse.ShowDocumentation.ToString()
            };
        }
        public ActivityResponseDTO GenerateErrorRepsonse(string errorMessage)
        {
            return new ActivityResponseDTO
            {
                Body = errorMessage,
                Type = ActivityResponse.ShowDocumentation.ToString()
            };
        }

        public SolutionPageDTO GetDefaultDocumentation(string solutionName, double solutionVersion, string terminalName, string body)
        {
            var curSolutionPage = new SolutionPageDTO
            {
                Name = solutionName,
                Version = solutionVersion,
                Terminal = terminalName,
                Body = body
            };

            return curSolutionPage;
        }

        /*******************************************************************************************/
        // Working with payload values
        /*******************************************************************************************/

        /// <summary>
        /// Extracts crate with specified label and ManifestType = Standard Design Time,
        /// then extracts field with specified fieldKey.
        /// </summary>
        protected string ExtractPayloadFieldValue(ICrateStorage crateStorage, string fieldKey, ActivityDO curActivity)
        {
            var fieldValues = crateStorage.CratesOfType<StandardPayloadDataCM>().SelectMany(x => x.Content.GetValues(fieldKey))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            if (fieldValues.Length > 0)
                return fieldValues[0];

            var baseEvent = new BaseTerminalEvent();
            var exceptionMessage = string.Format("No field found with specified key: {0}.", fieldKey);
            //This is required for proper logging of the Incidents
            var fr8UserId = curActivity.AuthorizationToken.UserID;
            baseEvent.SendTerminalErrorIncident(ActivityName, exceptionMessage, ActivityName, fr8UserId);

            throw new ApplicationException(exceptionMessage);
        }





        /*******************************************************************************************/
        // Working with upstream
        /*******************************************************************************************/

        //wrapper for support test method
        public virtual async Task<List<Crate<TManifest>>> GetCratesByDirection<TManifest>(ActivityDO activityDO, CrateDirection direction)
        {
            return await HubCommunicator.GetCratesByDirection<TManifest>(activityDO, direction, CurrentFr8UserId);
            // return await Activity.GetCratesByDirection<TManifest>(activityId, direction);
        }

        //wrapper for support test method
        public virtual async Task<List<Crate>> GetCratesByDirection(ActivityDO activityDO, CrateDirection direction)
        {
            return await HubCommunicator.GetCratesByDirection(activityDO, direction, CurrentFr8UserId);
        }

        public virtual async Task<FieldDescriptionsCM> GetDesignTimeFields(Guid activityId, CrateDirection direction, AvailabilityType availability = AvailabilityType.NotSet)
        {
            var mergedFields = await HubCommunicator.GetDesignTimeFieldsByDirection(activityId, direction, availability, CurrentFr8UserId);
            return mergedFields;
        }

        public virtual async Task<FieldDescriptionsCM> GetDesignTimeFields(ActivityDO activityDO, CrateDirection direction, AvailabilityType availability = AvailabilityType.NotSet)
        {
            var mergedFields = await HubCommunicator.GetDesignTimeFieldsByDirection(activityDO, direction, availability, CurrentFr8UserId);
            return mergedFields;
        }

        public virtual async Task<List<CrateManifestType>> BuildUpstreamManifestList(ActivityDO activityDO)
        {
            var upstreamCrates = await this.GetCratesByDirection<Data.Interfaces.Manifests.Manifest>(activityDO, CrateDirection.Upstream);
            return upstreamCrates.Where(x => !ExcludedManifestTypes.Contains(x.ManifestType)).Select(f => f.ManifestType).Distinct().ToList();
        }

        public virtual async Task<List<String>> BuildUpstreamCrateLabelList(ActivityDO activityDO)
        {
            var curCrates = await this.GetCratesByDirection<Data.Interfaces.Manifests.Manifest>(activityDO, CrateDirection.Upstream);
            return curCrates.Where(x => !ExcludedManifestTypes.Contains(x.ManifestType)).Select(f => f.Label).Distinct().ToList();
        }

        public virtual async Task<Crate<FieldDescriptionsCM>> GetUpstreamManifestListCrate(ActivityDO activityDO)
        {
            var manifestList = (await BuildUpstreamManifestList(activityDO));
            var fields = manifestList.Select(f => new FieldDTO(f.Type, f.Id.ToString())).ToArray();

            return CrateManager.CreateDesignTimeFieldsCrate("AvailableUpstreamManifests", fields);
        }

        public virtual async Task<Crate<FieldDescriptionsCM>> GetUpstreamCrateLabelListCrate(ActivityDO activityDO)
        {
            var labelList = (await BuildUpstreamCrateLabelList(activityDO));
            var fields = labelList.Select(f => new FieldDTO(f, f)).ToArray();

            return CrateManager.CreateDesignTimeFieldsCrate("AvailableUpstreamLabels", fields);
        }


        protected virtual async Task<List<Crate<StandardFileDescriptionCM>>> GetUpstreamFileHandleCrates(ActivityDO activityDO)
        {
            return await HubCommunicator.GetCratesByDirection<StandardFileDescriptionCM>(activityDO, CrateDirection.Upstream, CurrentFr8UserId);
        }

        protected async Task<Crate<FieldDescriptionsCM>> MergeUpstreamFields(ActivityDO activityDO, string label)
        {
            var curUpstreamFields = (await GetDesignTimeFields(activityDO.Id, CrateDirection.Upstream)).Fields.ToArray();
            var upstreamFieldsCrate = CrateManager.CreateDesignTimeFieldsCrate(label, curUpstreamFields);

            return upstreamFieldsCrate;
        }

        /// <summary>
        /// Creates a crate with available design-time fields.
        /// </summary>
        /// <param name="activityDO">ActionDO.</param>
        /// <returns></returns>
        protected async Task<Crate> CreateAvailableFieldsCrate(ActivityDO activityDO,
            string crateLabel = "Upstream Terminal-Provided Fields",
            AvailabilityType availabilityTypeUpstream = AvailabilityType.RunTime,
            AvailabilityType availabilityTypeFieldsCrate = AvailabilityType.Configuration)
        {
            var curUpstreamFields = await HubCommunicator.GetDesignTimeFieldsByDirection(activityDO, CrateDirection.Upstream, availabilityTypeUpstream, CurrentFr8UserId);

            if (curUpstreamFields == null)
            {
                curUpstreamFields = new FieldDescriptionsCM();
            }

            var availableFieldsCrate = CrateManager.CreateDesignTimeFieldsCrate(
                    crateLabel,
                    curUpstreamFields.Fields,
                    availabilityTypeFieldsCrate
                );

            return availableFieldsCrate;
        }
       
        // create crate with list of fields available upstream
        protected async Task<Crate<FieldDescriptionsCM>> CreateDesignTimeFieldsCrate(ActivityDO curActivityDO, string label)
        {
            List<Crate<FieldDescriptionsCM>> crates = null;

            try
            {
                //throws exception from test classes when it cannot call webservice
                crates = await GetCratesByDirection<FieldDescriptionsCM>(curActivityDO, CrateDirection.Upstream);
            }
            catch { }

            if (crates != null)
            {
                var upstreamFields = crates.SelectMany(x => x.Content.Fields).Where(a => a.Availability != AvailabilityType.Configuration).ToArray();

                return CrateManager.CreateDesignTimeFieldsCrate(label, upstreamFields);
            }

            return null;
        }
        
        /// <summary>
        /// Extract upstream data based on Upstream Crate Chooser Control's selected manifest and label 
        /// </summary>
        /// <param name="upstreamCrateChooserName">Upstream Crate Chooser Control's name</param>
        /// <param name="curActivityDO"></param>
        /// <returns>StandardTableDataCM. StandardTableDataCM.Table.Count = 0 if its empty</returns>
        protected async Task<StandardTableDataCM> ExtractDataFromUpstreamCrates(string upstreamCrateChooserName, ActivityDO curActivityDO)
        {
            var controls = GetConfigurationControls(curActivityDO);
            var crateChooser = controls.FindByName<UpstreamCrateChooser>(upstreamCrateChooserName);

            //Get selected manifest and label from crate chooser
            var manifestType = crateChooser.SelectedCrates.Where(w => !String.IsNullOrEmpty(w.ManifestType.Value)).Select(c => c.ManifestType.Value);
            var labelControl = crateChooser.SelectedCrates.Where(w => !String.IsNullOrEmpty(w.Label.Value)).Select(c => c.Label.Value);

            //filter by manifest or label based on selected crate chooser control
            Func<Crate, bool> whereClause = null;
            if (manifestType.Any() && labelControl.Any())
                whereClause = (crate => manifestType.Contains(crate.ManifestType.Id.ToString()) && labelControl.Contains(crate.Label));
            else if (manifestType.Any())
                whereClause = (crate => manifestType.Contains(crate.ManifestType.Id.ToString()));
            else if (labelControl.Any())
                whereClause = (crate => labelControl.Contains(crate.Label));


            //get upstream controls to extract the data based on the selected manifest or label
            var getUpstreamCrates = (await GetCratesByDirection(curActivityDO, CrateDirection.Upstream)).ToArray();
            var selectedUpcrateControls = getUpstreamCrates.Where(whereClause).Select(s => s);//filter upstream controls

            StandardTableDataCM resultTable = new StandardTableDataCM();
            foreach (var selectedUpcrateControl in selectedUpcrateControls)
            {
                if (selectedUpcrateControl != null)
                {
                    //for standard table data CM return directly the result - the rest of CM will be converted to TableDataCM
                    if (selectedUpcrateControl.IsOfType<StandardTableDataCM>())
                    {
                        var contentTableCM = selectedUpcrateControl.Get<StandardTableDataCM>();
                        resultTable.Table.AddRange(contentTableCM.Table);//add rows into results StandardTableDataCM
                    }
                    else
                        throw new NotSupportedException("Manifest type is not yet implemented in extracting the data.");
                }
            }

            return resultTable;
        }
      
        /*******************************************************************************************/
        // Working with controls
        /*******************************************************************************************/

        /// <summary>
        /// This is a generic function for creating a CrateChooser which is suitable for most use-cases
        /// </summary>
        /// <param name="curActivityDO"></param>
        /// <param name="label"></param>
        /// <param name="name"></param>
        /// <param name="singleManifest"></param>
        /// <returns></returns>
        protected async Task<CrateChooser> GenerateCrateChooser(
            ActivityDO curActivityDO,
            string name,
            string label,
            bool singleManifest,
            bool requestUpstream = false,
            bool requestConfig = false)
        {
            var crateDescriptions = await GetCratesByDirection<CrateDescriptionCM>(curActivityDO, CrateDirection.Upstream);
            var runTimeCrateDescriptions = crateDescriptions.Where(c => c.Availability == AvailabilityType.RunTime).SelectMany(c => c.Content.CrateDescriptions);
            var control = new CrateChooser
            {
                Label = label,
                Name = name,
                CrateDescriptions = runTimeCrateDescriptions.ToList(),
                SingleManifestOnly = singleManifest,
                RequestUpstream = requestUpstream
            };

            if (requestConfig)
            {
                control.Events.Add(new ControlEvent("onChange", "requestConfig"));
            }

            return control;
        }

        /// <summary>
        /// Creates StandardConfigurationControlsCM with TextSource control
        /// </summary>
        /// <param name="storage">Crate Storage</param>
        /// <param name="label">Initial Label for the text source control</param>
        /// <param name="controlName">Name of the text source control</param>
        /// <param name="upstreamSourceLabel">Label for the upstream source</param>
        /// <param name="filterByTag">Filter for upstream source, Empty by default</param>
        /// <param name="addRequestConfigEvent">True if onChange event needs to be configured, False otherwise. True by default</param>
        /// <param name="required">True if the control is required, False otherwise. False by default</param>
        protected void AddTextSourceControl(
            ICrateStorage storage,
            string label,
            string controlName,
            string upstreamSourceLabel,
            string filterByTag = "",
            bool addRequestConfigEvent = false,
            bool required = false,
            bool requestUpstream = false)
        {
            var textSourceControl = CreateSpecificOrUpstreamValueChooser(
                label,
                controlName,
                upstreamSourceLabel,
                filterByTag,
                addRequestConfigEvent,
                requestUpstream
            );
            textSourceControl.Required = required;

            AddControl(storage, textSourceControl);
        }

        /// <summary>
        /// Adds Text Source for the DTO type. 
        /// </summary>
        /// <remarks>The (T), DTO's Proerty Names will be used to name and label the new Text Source Controls</remarks>
        protected void AddTextSourceControlForDTO<T>(
            ICrateStorage storage,
            string upstreamSourceLabel,
            string filterByTag = "",
            bool addRequestConfigEvent = false,
            bool required = false,
            bool requestUpstream = false)
        {
            typeof(T).GetProperties()
                .Where(property => !property.Name.Equals("Id")).ToList().ForEach(property =>
                {
                    AddTextSourceControl(
                        storage,
                        property.Name,
                        property.Name,
                        upstreamSourceLabel,
                        filterByTag,
                        addRequestConfigEvent,
                        required,
                        requestUpstream
                    );
                });
        }

        /// <summary>
        /// Creates RadioButtonGroup to enter specific value or choose value from upstream crate.
        /// </summary>
        protected TextSource CreateSpecificOrUpstreamValueChooser(
            string label,
            string controlName,
            string upstreamSourceLabel = "",
            string filterByTag = "",
            bool addRequestConfigEvent = false,
            bool requestUpstream = false)
        {
            var control = new TextSource(label, upstreamSourceLabel, controlName)
            {
                Source = new FieldSourceDTO
                {
                    Label = upstreamSourceLabel,
                    ManifestType = CrateManifestTypes.StandardDesignTimeFields,
                    FilterByTag = filterByTag,
                    RequestUpstream = requestUpstream
                }
            };
            if (addRequestConfigEvent)
            {
                control.Events.Add(new ControlEvent("onChange", "requestConfig"));
            }

            return control;
        }

        protected UpstreamCrateChooser CreateUpstreamCrateChooser(string name, string label, bool isMultiSelection = true)
        {

            var manifestDdlb = new DropDownList { Name = name + "_mnfst_dropdown_0", Source = new FieldSourceDTO(CrateManifestTypes.StandardDesignTimeFields, "AvailableUpstreamManifests") };
            var labelDdlb = new DropDownList { Name = name + "_lbl_dropdown_0", Source = new FieldSourceDTO(CrateManifestTypes.StandardDesignTimeFields, "AvailableUpstreamLabels") };

            var ctrl = new UpstreamCrateChooser
            {
                Name = name,
                Label = label,
                SelectedCrates = new List<CrateDetails> { new CrateDetails { Label = labelDdlb, ManifestType = manifestDdlb } },
                MultiSelection = isMultiSelection
            };

            return ctrl;
        }


        /// <summary>
        /// Creates TextBlock control and fills it with label, value and CssClass
        /// </summary>
        /// <param name="curLabel">Label</param>
        /// <param name="curValue">Value</param>
        /// <param name="curCssClass">Css Class</param>
        /// <param name="curName">Name</param>
        /// <returns>TextBlock control</returns>
        protected TextBlock GenerateTextBlock(string curLabel, string curValue, string curCssClass, string curName = "unnamed")
        {
            return new TextBlock
            {
                Name = curName,
                Label = curLabel,
                Value = curValue,
                CssClass = curCssClass
            };
        }
       
        /*******************************************************************************************/
        // Deprecated methods
        // Can be refactored if necessary
        /*******************************************************************************************/

        //looks for the Configuration Controls Crate and Extracts the ManifestSchema
        protected StandardConfigurationControlsCM GetControlsManifest(ActivityDO curActivity)
        {
            var control = CrateManager.GetStorage(curActivity.CrateStorage).CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            if (control == null)
            {
                throw new ApplicationException(string.Format("No crate found with Label == \"Configuration_Controls\" and ManifestType == \"{0}\"", CrateManifestTypes.StandardConfigurationControls));
            }

            return control;
        }

        protected ControlDefinitionDTO GetControl(StandardConfigurationControlsCM controls, string name, string controlType = null)
        {
            Func<ControlDefinitionDTO, bool> predicate = x => x.Name == name;
            if (controlType != null)
            {
                predicate = x => x.Type == controlType && x.Name == name;
            }

            return controls.Controls.FirstOrDefault(predicate);
        }

        protected T GetControl<T>(StandardConfigurationControlsCM controls, string name, string controlType = null) where T : ControlDefinitionDTO
        {
            return (T)GetControl(controls, name, controlType);
        }

        protected ControlDefinitionDTO GetControl(ActivityDO curActivityDO, string name, string controlType = null)
        {
            var controls = GetConfigurationControls(curActivityDO);
            return GetControl(controls, name, controlType);
        }

        // do not use after EnhancedTerminalActivity is introduced
        protected ICrateStorage AssembleCrateStorage(params Crate[] curCrates)
        {
            return new CrateStorage(curCrates);
        }

        // do not use after EnhancedTerminalActivity is introduced
        protected Crate PackControls(StandardConfigurationControlsCM page)
        {
            return PackControlsCrate(page.Controls.ToArray());
        }

        // do not use after EnhancedTerminalActivity is introduced
        protected Crate<StandardConfigurationControlsCM> PackControlsCrate(params ControlDefinitionDTO[] controlsList)
        {
            return Crate<StandardConfigurationControlsCM>.FromContent(ConfigurationControlsLabel, new StandardConfigurationControlsCM(controlsList), AvailabilityType.Configuration);
        }

        // do not use after EnhancedTerminalActivity is introduced
        protected string ExtractControlFieldValue(ActivityDO curActivityDO, string fieldName)
        {
            var storage = CrateManager.GetStorage(curActivityDO);

            var controlsCrateMS = storage.CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            //            var controlsCrate = curActivityDto.CrateStorage.CrateDTO
            //                .FirstOrDefault(
            //                    x => x.ManifestType == CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME
            //                    && x.Label == "Configuration_Controls");
            //
            //            if (controlsCrate == null)
            //            {
            //                throw new ApplicationException("No Configuration_Controls crate found.");
            //            }
            //
            //            var controlsCrateMS = JsonConvert
            //                .DeserializeObject<StandardConfigurationControlsCM>(
            //                    controlsCrate.Contents
            //                );

            var field = controlsCrateMS.Controls
                .FirstOrDefault(x => x.Name == fieldName);

            if (field == null)
            {
                return null;
            }

            return field.Value;
        }


        protected void AddLabelControl(ICrateStorage storage, string name, string label, string text)
        {
            AddControl(
                storage,
                GenerateTextBlock(label, text, "well well-lg", name)
            );
        }

        protected void AddControl(ICrateStorage storage, ControlDefinitionDTO control)
        {
            var controlsCrate = EnsureControlsCrate(storage);

            if (controlsCrate.Content == null) { return; }

            controlsCrate.Content.Controls.Add(control);
        }

        protected void InsertControlAfter(ICrateStorage storage, ControlDefinitionDTO control, string afterControlName)
        {
            var controlsCrate = EnsureControlsCrate(storage);

            if (controlsCrate.Content == null) { return; }

            for (var i = 0; i < controlsCrate.Content.Controls.Count; ++i)
            {
                if (controlsCrate.Content.Controls[i].Name == afterControlName)
                {
                    if (i == controlsCrate.Content.Controls.Count - 1)
                    {
                        controlsCrate.Content.Controls.Add(control);
                    }
                    else
                    {
                        controlsCrate.Content.Controls.Insert(i + 1, control);
                    }

                    break;
                }
            }
        }

        protected ControlDefinitionDTO FindControl(ICrateStorage storage, string name)
        {
            var controlsCrate = storage.CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            if (controlsCrate == null) { return null; }

            var control = controlsCrate.Controls
                .FirstOrDefault(x => x.Name == name);

            return control;
        }


        /// <summary>
        /// specify selected FieldDO for DropDownList
        /// specify Value (TimeSpan) for Duration
        /// </summary>
        protected void SetControlValue(ActivityDO activity, string controlFullName, object value)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(activity))
            {
                var controls = crateStorage
                    .CrateContentsOfType<StandardConfigurationControlsCM>()
                    .FirstOrDefault();

                if (controls != null)
                {
                    var control = TraverseNestedControls(controls.Controls, controlFullName);

                    if (control != null)
                    {
                        switch (control.Type)
                        {
                            case "TextBlock":
                            case "TextBox":
                            case ControlTypes.TextArea:
                                control.Value = (string)value;
                                break;
                            case "CheckBox":
                                control.Selected = true;
                                break;
                            case "DropDownList":
                                var ddlb = control as DropDownList;
                                var val = value as ListItem;
                                ddlb.selectedKey = val.Key;
                                ddlb.Value = val.Value;
                                //ddlb.ListItems are not loaded yet
                                break;
                            case "Duration":
                                var duration = control as Duration;
                                var timespan = (TimeSpan)value;
                                duration.Days = timespan.Days;
                                duration.Hours = timespan.Hours;
                                duration.Minutes = timespan.Minutes;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
            }
        }

        private ControlDefinitionDTO TraverseNestedControls(List<ControlDefinitionDTO> controls, string childControl)
        {
            ControlDefinitionDTO controlDefinitionDTO = null;
            var controlNames = childControl.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (controlNames.Length > 0 && controls.Count > 0)
            {
                var control = controls.FirstOrDefault(a => a.Name == controlNames[0]);

                if (control != null)
                {
                    if (controlNames.Count() == 1)
                    {
                        controlDefinitionDTO = control;
                    }
                    else
                    {
                        List<ControlDefinitionDTO> nestedControls = null;

                        if (control.Type == "RadioButtonGroup")
                        {
                            var radio = (control as RadioButtonGroup).Radios.FirstOrDefault(a => a.Name == controlNames[1]);
                            if (radio != null)
                            {
                                radio.Selected = true;
                                nestedControls = radio.Controls.ToList();

                                controlDefinitionDTO = TraverseNestedControls(nestedControls, string.Join(".", controlNames.Skip(2)));
                            }
                        }
                        //TODO: Add support for future controls with nested child controls
                        else
                            throw new NotImplementedException("Can't search for controls inside of " + control.Type);
                    }
                }
            }

            return controlDefinitionDTO;
        }

        protected void RemoveControl(ICrateStorage storage, string name)
        {
            var controlsCrate = storage.CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            if (controlsCrate == null) { return; }


            var control = controlsCrate.Controls.FirstOrDefault(x => x.Name == name);

            if (control != null)
            {
                controlsCrate.Controls.Remove(control);
            }
        }

        protected Crate<StandardConfigurationControlsCM> EnsureControlsCrate(ICrateStorage storage)
        {
            var controlsCrate = storage.CratesOfType<StandardConfigurationControlsCM>().FirstOrDefault();

            if (controlsCrate == null)
            {
                controlsCrate = CrateManager.CreateStandardConfigurationControlsCrate(ConfigurationControlsLabel);
                storage.Add(controlsCrate);
            }

            return controlsCrate;
        }

        public string ParseConditionToText(List<FilterConditionDTO> filterData)
        {
            var parsedConditions = new List<string>();

            filterData.ForEach(condition =>
            {
                string parsedCondition = condition.Field;

                switch (condition.Operator)
                {
                    case "eq":
                        parsedCondition += " = ";
                        break;
                    case "neq":
                        parsedCondition += " != ";
                        break;
                    case "gt":
                        parsedCondition += " > ";
                        break;
                    case "gte":
                        parsedCondition += " >= ";
                        break;
                    case "lt":
                        parsedCondition += " < ";
                        break;
                    case "lte":
                        parsedCondition += " <= ";
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Not supported operator: {0}", condition.Operator));
                }

                parsedCondition += string.Format("'{0}'", condition.Value);
                parsedConditions.Add(parsedCondition);
            });

            var finalCondition = string.Join(" AND ", parsedConditions);

            return finalCondition;
        }

        // do not use after EnhancedTerminalActivity is introduced
        protected StandardConfigurationControlsCM GetConfigurationControls(ActivityDO curActivityDO)
        {
            var storage = CrateManager.GetStorage(curActivityDO);
            return GetConfigurationControls(storage);
        }

        // do not use after EnhancedTerminalActivity is introduced
        protected StandardConfigurationControlsCM GetConfigurationControls(ICrateStorage storage)
        {
            var controlsCrate = storage.CrateContentsOfType<StandardConfigurationControlsCM>(c => c.Label == ConfigurationControlsLabel).FirstOrDefault();
            return controlsCrate;
        }

        // do not use after EnhancedTerminalActivity is introduced
        /// <summary>
        /// Method to be used with Loop Action
        /// Is a helper method to decouple some of the GetCurrentElement Functionality
        /// </summary>
        /// <param name="operationalCrate">Crate of the OperationalStateCM</param>
        /// <param name="loopId">Integer that is equal to the Action.Id</param>
        /// <returns>Index or pointer of the current IEnumerable Object</returns>
        protected int GetLoopIndex(OperationalStateCM operationalCrate, string loopId)
        {
            var curLoop = operationalCrate.Loops.FirstOrDefault(l => l.Id.Equals(loopId.ToString()));
            if (curLoop == null)
            {
                throw new NullReferenceException("No Loop with the specified LoopId inside the provided OperationalStateCM crate");
            }
            var curIndex = curLoop.Index;
            return curIndex;
        }

        // do not use after EnhancedTerminalActivity is introduced
        /// <summary>
        /// Trivial method to return element at specified index of the IEnumerable object.
        /// To be used with Loop Action.
        /// IMPORTANT: 
        /// 1) Index update is performed by Loop Action
        /// 2) Loop brake is preformed by Loop Action
        /// </summary>
        /// <param name="enumerableObject">Object of type IEnumerable</param>
        /// <param name="objectIndex">Integer that points to the element</param>
        /// <returns>Object of any type</returns>
        protected object GetCurrentElement(IEnumerable<object> enumerableObject, int objectIndex)
        {
            var curElement = enumerableObject.ElementAt(objectIndex);
            return curElement;
        }
    }
}