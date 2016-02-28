﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Hub.Managers;
using Newtonsoft.Json;
using StructureMap;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.Repositories;
using Data.States;
using Utilities;
using terminalFr8Core;
using terminalFr8Core.Infrastructure;
using terminalFr8Core.Interfaces;
using terminalFr8Core.Services;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using TerminalBase.Services;
using System.Text.RegularExpressions;
using Hub.Infrastructure;
using Utilities.Logging;

namespace terminalFr8Core.Actions
{
    public class SearchFr8Warehouse_v1 : BaseTerminalActivity
    {
        private const string QueryCrateLabel = "Fr8 Search Query";
        private const string SolutionName = "Search Fr8 Warehouse";
        private const double SolutionVersion = 1.0;
        private const string TerminalName = "terminalFr8Core";
        private const string SolutionBody = @"<p>The Search Fr8 Warehouse solution allows you to search the Fr8 Warehouse 
                                            for information we're storing for you. This might be event data about your cloud services that we track on your 
                                            behalf. Or it might be files or data that your plans have stored.</p>";

        // Here in this action we have query builder control to build queries against MT database.
        // Note We are ignoring the generic type searching and fetching  FR-2317

        public class ActionUi : StandardConfigurationControlsCM
        {
            [JsonIgnore]
            public QueryBuilder QueryBuilder { get; set; }

            public ActionUi()
            {
                Controls = new List<ControlDefinitionDTO>();

                Controls.Add(new TextArea
                {
                    IsReadOnly = true,
                    Label = "",
                    Value = "<p>Search for Fr8 Warehouse where the following are true:</p>"
                });

                Controls.Add(new DropDownList()
                {
                    Name = "Select Fr8 Warehouse Object",
                    Required = true,
                    Label = "Select Fr8 Warehouse Object",
                    Source = new FieldSourceDTO
                    {
                        Label = "Queryable Objects",
                        ManifestType = CrateManifestTypes.StandardDesignTimeFields
                    },
                    Events = new List<ControlEvent> { new ControlEvent("onChange", "requestConfig") }
                });

                Controls.Add((QueryBuilder = new QueryBuilder
                {
                    Name = "QueryBuilder",
                    Value = null,
                    Source = new FieldSourceDTO
                    {
                        Label = "Queryable Criteria",
                        ManifestType = CrateManifestTypes.StandardDesignTimeFields
                    }
                }));

                Controls.Add(new Button()
                {
                    Label = "Continue",
                    Name = "Continue",
                    Events = new List<ControlEvent>()
                    {
                        new ControlEvent("onClick", "requestConfig")
                    }
                });
            }
        }

        public SearchFr8Warehouse_v1()
        {
        }

        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payload = await GetPayload(curActivityDO, containerId);
            return Success(payload);
        }

        public override async Task<PayloadDTO> ExecuteChildActivities(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payload = await GetPayload(curActivityDO, containerId);

            var configurationControls = CrateManager.GetStorage(curActivityDO).CrateContentsOfType<StandardConfigurationControlsCM>().SingleOrDefault();

            if (configurationControls == null)
            {
                return Error(payload, "Action was not configured correctly");
            }

            // Merge data from QueryMT action.
            var payloadCrateStorage = CrateManager.FromDto(payload.CrateStorage);
            var queryMTResult = payloadCrateStorage
                .CrateContentsOfType<StandardPayloadDataCM>(x => x.Label == "Found MT Objects")
                .FirstOrDefault();

            using (var crateStorage = CrateManager.GetUpdatableStorage(payload))
            {
                crateStorage.Add(Data.Crates.Crate.FromContent("Sql Query Result", queryMTResult));
            }

            return ExecuteClientActivity(payload, "ShowTableReport");
        }


        protected override Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Add(PackControls(new ActionUi()));
                var designTimefieldLists = GetFr8WarehouseObject(authTokenDO);
                var availableMtObjects = CrateManager.CreateDesignTimeFieldsCrate("Queryable Objects", designTimefieldLists.ToArray());
                crateStorage.Add(availableMtObjects);
                crateStorage.AddRange(PackDesignTimeData());
            }
            return Task.FromResult(curActivityDO);
        }

        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO activityDO, AuthorizationTokenDO authTokenDO)
        {
            try
            {
                var configurationcontrols = GetConfigurationControls(activityDO);
                var fr8ObjectDropDown = GetControl(configurationcontrols, "Select Fr8 Warehouse Object");
                var fr8ObjectID = fr8ObjectDropDown.Value;
                var continueButton = configurationcontrols.FindByName<Button>("Continue");

                if (ButtonIsClicked(continueButton))
                {
                    if (!ValidateSolutionInputs(fr8ObjectID))
                    {
                        AddRemoveCrateAndError(activityDO, fr8ObjectID, "Please select the Fr8 Object");
                        return activityDO;
                    }
                    else {
                        AddRemoveCrateAndError(activityDO, fr8ObjectID, "");
                    }

                    activityDO.ChildNodes.Clear();
                    await GenerateSolutionActivities(activityDO, fr8ObjectID);
                    UpdateOperationCrate(activityDO);
                }
                else {
                    LoadAvailableFr8ObjectNames(activityDO, fr8ObjectID);
                }
            }
            catch (Exception e)
            {
                // This message will get display in Terminal Activity Response.
                Logger.GetLogger().Error("Error while configuring the search Fr8 Warehouse action" + e.Message, e);
                throw;
            }

            return activityDO;
        }

        /// <summary>
        /// This method provides documentation in two forms:
        /// SolutionPageDTO for general information and 
        /// ActivityResponseDTO for specific Help on minicon
        /// </summary>
        /// <param name="activityDO"></param>
        /// <param name="curDocumentation"></param>
        /// <returns></returns>
        public dynamic Documentation(ActivityDO activityDO, string curDocumentation)
        {
            if (curDocumentation.Contains("MainPage"))
            {
                var curSolutionPage = GetDefaultDocumentation(SolutionName, SolutionVersion, TerminalName, SolutionBody);
                return Task.FromResult(curSolutionPage);
            }
            return
                Task.FromResult(
                    GenerateErrorRepsonce("Unknown displayMechanism: we currently support MainPage cases"));
        }

        protected async Task<ActivityDO> GenerateSolutionActivities(ActivityDO activityDO, string fr8ObjectID)
        {
            var queryFr8WarehouseAction = await AddAndConfigureChildActivity(
                activityDO,
                "QueryFr8Warehouse"
            );

            using (var crateStorage = CrateManager.GetUpdatableStorage(queryFr8WarehouseAction))
            {
                // We insteady of using getConfiguration control used the same GetConfiguration control required actionDO
                var queryFr8configurationControls = crateStorage.
                    CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();

                var radioButtonGroup = queryFr8configurationControls
                    .FindByName<RadioButtonGroup>("QueryPicker");

                DropDownList fr8ObjectDropDown = null;

                if (radioButtonGroup != null
                    && radioButtonGroup.Radios.Count > 0
                    && radioButtonGroup.Radios[0].Controls.Count > 0)
                {
                    fr8ObjectDropDown = radioButtonGroup.Radios[1].Controls[0] as DropDownList;
                    radioButtonGroup.Radios[1].Selected = true;
                    radioButtonGroup.Radios[0].Selected = false;
                }

                if (fr8ObjectDropDown != null)
                {
                    fr8ObjectDropDown.Selected = true;
                    fr8ObjectDropDown.Value = fr8ObjectID;
                    fr8ObjectDropDown.selectedKey = fr8ObjectID;

                    FilterPane upstreamCrateChooser1 = radioButtonGroup.Radios[1].Controls[1] as FilterPane;

                    var configurationControls = GetConfigurationControls(activityDO);
                    var queryBuilderControl = configurationControls.FindByName<QueryBuilder>("QueryBuilder");
                    var criteria = JsonConvert.DeserializeObject<List<FilterConditionDTO>>(queryBuilderControl.Value);

                    FilterDataDTO filterPaneDTO = new FilterDataDTO();
                    filterPaneDTO.Conditions = criteria;
                    filterPaneDTO.ExecutionType = FilterExecutionType.WithFilter;
                    upstreamCrateChooser1.Value = JsonConvert.SerializeObject(filterPaneDTO);
                    upstreamCrateChooser1.Selected = true;
                }
            }

            queryFr8WarehouseAction = await ConfigureChildActivity(
                activityDO,
                queryFr8WarehouseAction
            );

            return activityDO;
        }

        private void LoadAvailableFr8ObjectNames(ActivityDO actvityDO, string fr8ObjectID)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(actvityDO))
            {
                var designTimeQueryFields = GetFr8WarehouseFieldNames(fr8ObjectID);
                var criteria = crateStorage.FirstOrDefault(d => d.Label == "Queryable Criteria");
                if (criteria != null)
                {
                    crateStorage.Remove(criteria);
                }
                crateStorage.Add(Data.Crates.Crate.FromContent("Queryable Criteria", new StandardQueryFieldsCM(designTimeQueryFields)));
            }
        }

        private void UpdateOperationCrate(ActivityDO activityDO, string errorMessage = null)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(activityDO))
            {
                crateStorage.RemoveByManifestId((int)MT.OperationalStatus);
                var operationalStatus = new OperationalStateCM();

                operationalStatus.CurrentActivityResponse =
                    ActivityResponseDTO.Create(ActivityResponse.ExecuteClientActivity);
                operationalStatus.CurrentClientActivityName = "RunImmediately";
                var operationsCrate = Data.Crates.Crate.FromContent("Operational Status", operationalStatus);
                crateStorage.Add(operationsCrate);
            }
        }

        private void AddRemoveCrateAndError(ActivityDO activityDO,string fr8ObjectID,string errorMessage)
        {
            using (var crateStorage = CrateManager.GetUpdatableStorage(activityDO))
            {
                crateStorage.Remove<StandardQueryCM>();
                var queryCrate = ExtractQueryCrate(crateStorage, fr8ObjectID);
                crateStorage.Add(queryCrate);

                var configurationcontrols = crateStorage.
                  CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();
                var fr8ObjectDropDown = GetControl(configurationcontrols, "Select Fr8 Warehouse Object");
                fr8ObjectDropDown.ErrorMessage = errorMessage;
            }
        }

        private bool ValidateSolutionInputs(string fr8Object)
        {
            if (String.IsNullOrWhiteSpace(fr8Object))
            {
                return false;
            }
            return true;
        }

        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            if (CrateManager.IsStorageEmpty(curActivityDO))
            {
                return ConfigurationRequestType.Initial;
            }
            return ConfigurationRequestType.Followup;
        }

        private static ControlDefinitionDTO CreateTextBoxQueryControl(
            string key)
        {
            return new TextBox()
            {
                Name = "QueryField_" + key
            };
        }

        private Crate<StandardQueryCM> ExtractQueryCrate(ICrateStorage storage, string mtObject)
        {
            var configurationControls = storage
                .CrateContentsOfType<StandardConfigurationControlsCM>()
                .SingleOrDefault();

            if (configurationControls == null)
            {
                throw new ApplicationException("Action was not configured correctly");
            }

            var actionUi = new ActionUi();
            actionUi.ClonePropertiesFrom(configurationControls);

            var criteria = JsonConvert.DeserializeObject<List<FilterConditionDTO>>(
                actionUi.QueryBuilder.Value
            );

            var queryCM = new StandardQueryCM(
                new QueryDTO()
                {
                    Name = mtObject,
                    Criteria = criteria
                }
            );

            return Crate<StandardQueryCM>.FromContent(QueryCrateLabel, queryCM);
        }

        private IEnumerable<Crate> PackDesignTimeData()
        {
            yield return Data.Crates.Crate.FromContent("Fr8 Search Report", new FieldDescriptionsCM(new FieldDTO
            {
                Key = "Fr8 Search Report",
                Value = "Table",
                Availability = AvailabilityType.RunTime
            }));
        }

        // search MT Object into DB
        private MT_Object GetFr8Object(
           string fr8Object)
        {
            int id;

            Int32.TryParse(fr8Object, out id);
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var obj = uow.MTObjectRepository
                    .GetQuery()
                    .FirstOrDefault(x => x.Id == id);

                if (obj == null)
                {
                    return null;
                }

                return obj;
            }
        }

        // create the dropdown design time fields.
        private List<FieldDTO> GetFr8WarehouseObject(AuthorizationTokenDO oAuthToken)
        {
            using (var unitWork = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curFr8ObjectResult = unitWork.MTDataRepository.FindList(d => d.fr8AccountId == oAuthToken.UserID);

                var listFieldDTO = new List<FieldDTO>();

                foreach (var item in curFr8ObjectResult)
                {
                    var fr8ObjectRepository = unitWork.MTObjectRepository.GetQuery().FirstOrDefault(d => d.Id == item.MT_ObjectId);

                    if (!listFieldDTO.Exists(d => d.Key == fr8ObjectRepository.Name))
                    {
                        listFieldDTO.Add(new FieldDTO()
                        {
                            Key = fr8ObjectRepository.Name,
                            Value = fr8ObjectRepository.Id.ToString()
                        });
                    }
                }
                return listFieldDTO;
            }
        }

        // create the Query design time fields.
        private List<QueryFieldDTO> GetFr8WarehouseFieldNames(string fr8ObjectName)
        {
            List<QueryFieldDTO> designTimeQueryFields = new List<QueryFieldDTO>();
            var mtObject = GetFr8Object(fr8ObjectName);

            using (var unitWork = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                foreach (var field in unitWork.MTFieldRepository.GetQuery().Where(x => x.MT_ObjectId == mtObject.Id))
                {
                    if (!designTimeQueryFields.Exists(d => d.Name == field.Name))
                    {
                        designTimeQueryFields.Add(new QueryFieldDTO()
                        {
                            FieldType = QueryFieldType.String,
                            Label = field.Name,
                            Name = field.Name,
                            Control = CreateTextBoxQueryControl(field.Name)
                        });
                    }
                }
            }
            return designTimeQueryFields;
        }

        private bool ButtonIsClicked(Button button)
        {
            if (button != null && button.Clicked)
            {
                return true;
            }
            return false;
        }

    }
}