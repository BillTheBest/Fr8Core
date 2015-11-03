﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Enums;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;

namespace terminalFr8Core.Actions
{
    public class BuildQuery_v1 : BasePluginAction
    {
        #region Configuration.

        public override ConfigurationRequestType ConfigurationEvaluator(
            ActionDTO curActionDTO)
        {
            if (curActionDTO.CrateStorage == null
                || curActionDTO.CrateStorage.CrateDTO == null
                || curActionDTO.CrateStorage.CrateDTO.Count == 0)
            {
                return ConfigurationRequestType.Initial;
            }

            var controlsCrate = curActionDTO.CrateStorage.CrateDTO
                .FirstOrDefault(x => x.ManifestType == CrateManifests.STANDARD_CONF_CONTROLS_MANIFEST_NAME);

            if (controlsCrate == null)
            {
                return ConfigurationRequestType.Initial;
            }

            var controls = JsonConvert.DeserializeObject<StandardConfigurationControlsCM>(
                controlsCrate.Contents);

            var hasSelectObjectDdl = controls.Controls
                .Any(x => x.Name == "SelectObjectDdl");

            if (!hasSelectObjectDdl)
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected override async Task<ActionDTO> InitialConfigurationResponse(
            ActionDTO curActionDTO)
        {
            RemoveErrorLabel(curActionDTO, "UpstreamError");

            var tableDefinitions = await ExtractTableDefinitions(curActionDTO);
            List<FieldDTO> tablesList = null;

            if (tableDefinitions != null)
            {
                tablesList = ExtractTableNames(tableDefinitions);
            }

            if (tablesList == null || tablesList.Count == 0)
            {
                AddErrorLabel(
                    curActionDTO,
                    "UpstreamError",
                    "Unexpected error",
                    "No upstream crates found to extract table definitions."
                );
                return curActionDTO;
            }
            
            // var tablesCrate = Crate.CreateDesignTimeFieldsCrate("")

            return curActionDTO;
        }

        protected override Task<ActionDTO> FollowupConfigurationResponse(
            ActionDTO curActionDTO)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Returns list of FieldDTO from upper crate from ConnectToSql action.
        /// </summary>
        private async Task<List<FieldDTO>> ExtractTableDefinitions(ActionDTO actionDTO)
        {
            var upstreamCrates = await GetCratesByDirection(
                actionDTO.Id,
                CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME,
                GetCrateDirection.Upstream
            );

            if (upstreamCrates == null) { return null; }

            var tablesDefinitionCrate = upstreamCrates
                .FirstOrDefault(x => x.Label == "Sql Table Definitions");

            if (tablesDefinitionCrate == null) { return null; }

            var tablesDefinition = JsonConvert
                .DeserializeObject<StandardDesignTimeFieldsCM>(tablesDefinitionCrate.Contents);

            if (tablesDefinition == null) { return null; }

            return tablesDefinition.Fields;
        }

        /// <summary>
        /// Returns distinct list of table names from Table Definitions list.
        /// </summary>
        private List<FieldDTO> ExtractTableNames(List<FieldDTO> tableDefinitions)
        {
            var tables = new HashSet<string>();

            foreach (var column in tableDefinitions)
            {
                var columnTokens = column.Key
                    .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                string tableName;
                if (columnTokens.Length == 2)
                {
                    tableName = columnTokens[0];
                }
                else if (columnTokens.Length == 3)
                {
                    tableName = string.Format("{0}.{1}", columnTokens[0], columnTokens[1]);
                }
                else
                {
                    throw new NotSupportedException("Invalid column name.");
                }

                tables.Add(tableName);
            }

            var result = tables
                .Select(x => new FieldDTO() { Key = x, Value = x })
                .OrderBy(x => x.Key)
                .ToList();

            return result;
        }

        #endregion Configuration.

        #region Execution.

        public Task<PayloadDTO> Run(ActionDTO curActionDTO)
        {
            return Task.FromResult<PayloadDTO>(null);
        }

        #endregion Execution.
    }
}