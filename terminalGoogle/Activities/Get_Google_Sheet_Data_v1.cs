﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Managers;
using Newtonsoft.Json;
using terminalGoogle.DataTransferObjects;
using terminalGoogle.Interfaces;
using terminalGoogle.Services;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Utilities;

namespace terminalGoogle.Actions
{
    public class Get_Google_Sheet_Data_v1 : BaseTerminalActivity
    {
        private readonly IGoogleSheet _google;
        private const string TableCrateLabelPrefix = "Data from ";
        public Get_Google_Sheet_Data_v1()
        {
            _google = new GoogleSheet();
        }

        protected bool NeedsAuthentication(AuthorizationTokenDO authTokenDO)
        {
            if (authTokenDO == null) return true;
            if (!base.NeedsAuthentication(authTokenDO))
                return false;
            var token = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
            // we may also post token to google api to check its validity
            return (token.Expires - DateTime.Now > TimeSpan.FromMinutes(5) ||
                    !string.IsNullOrEmpty(token.RefreshToken));
        }

        public override Task<ActivityDO> Configure(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            CheckAuthentication(authTokenDO);

            return base.Configure(curActivityDO, authTokenDO);
        }


        /// <summary>
        /// Action processing infrastructure.
        /// </summary>
        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var payloadCrates = await GetPayload(curActivityDO, containerId);

            if (NeedsAuthentication(authTokenDO))
            {
                return NeedsAuthenticationError(payloadCrates);
            }

            ///// ********** This code is what have to be done by FR-2246 **************
            var dropDownListControl =
                (DropDownList)GetControlsManifest(curActivityDO).FindByName("select_spreadsheet");
            //get the spreadsheet name
            var spreadsheetName = dropDownListControl.selectedKey;
            //get the link to spreadsheet
            var spreadsheetsFromUserSelection = dropDownListControl.Value;
            var authDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
            //get the data
            var data = _google.EnumerateDataRows(spreadsheetsFromUserSelection, authDTO);
            var crate = CrateManager.CreateStandardTableDataCrate(TableCrateLabelPrefix + spreadsheetName, true, data.ToArray());
            using (var crateStorage = CrateManager.GetUpdatableStorage(payloadCrates))
            {
                crateStorage.Add(crate);
            }

            return Success(payloadCrates);

            ///// **********************The code below should be removed in the scope of FR-2246*************************************************

            //return await CreateStandardPayloadDataFromStandardTableData(curActivityDO, containerId, payloadCrates, authTokenDO);
        }



        private async Task<PayloadDTO> CreateStandardPayloadDataFromStandardTableData(ActivityDO curActivityDO, Guid containerId, PayloadDTO payloadCrates, AuthorizationTokenDO authTokenDO)
        {
            //at run time pull the entire sheet and store it as payload
            var spreadsheetControl = FindControl(CrateManager.GetStorage(curActivityDO.CrateStorage), "select_spreadsheet");
            var spreadsheetsFromUserSelection = string.Empty;
            if (spreadsheetControl != null)
                spreadsheetsFromUserSelection = spreadsheetControl.Value;

            if (!string.IsNullOrEmpty(spreadsheetsFromUserSelection))
            {
                curActivityDO = TransformSpreadsheetDataToPayloadDataCrate(curActivityDO, authTokenDO, spreadsheetsFromUserSelection);
            }

            var tableDataMS = await GetTargetTableData(curActivityDO);

            // Create a crate of payload data by using Standard Table Data manifest and use its contents to tranform into a Payload Data manifest.
            // Add a crate of PayloadData to action's crate storage
            var payloadDataCrate = CrateManager.CreatePayloadDataCrate("ExcelTableRow", "Excel Data", tableDataMS);

            using (var crateStorage = CrateManager.GetUpdatableStorage(payloadCrates))
            {
                crateStorage.Add(payloadDataCrate);
            }


            return Success(payloadCrates);
        }


        private async Task<StandardTableDataCM> GetTargetTableData(ActivityDO curActivityDO)
        {
            // Find crates of manifest type Standard Table Data
            var storage = CrateManager.GetStorage(curActivityDO);
            var standardTableDataCrates = storage.CratesOfType<StandardTableDataCM>().ToArray();

            // If no crate of manifest type "Standard Table Data" found, try to find upstream for a crate of type "Standard File Handle"
            if (!standardTableDataCrates.Any())
            {
                return await GetUpstreamTableData(curActivityDO);
            }

            return standardTableDataCrates.First().Content;
        }

        private async Task<StandardTableDataCM> GetUpstreamTableData(ActivityDO curActivityDO)
        {
            var upstreamFileHandleCrates = await GetUpstreamFileHandleCrates(curActivityDO);

            //if no "Standard File Handle" crate found then return
            if (!upstreamFileHandleCrates.Any())
                throw new Exception("No Standard File Handle crate found in upstream.");

            //if more than one "Standard File Handle" crates found then throw an exception
            if (upstreamFileHandleCrates.Count > 1)
                throw new Exception("More than one Standard File Handle crates found upstream.");

            throw new NotImplementedException();
            // Deserialize the Standard File Handle crate to StandardFileHandleMS object
            //StandardFileHandleMS fileHandleMS = JsonConvert.DeserializeObject<StandardFileHandleMS>(upstreamFileHandleCrates.First().Contents);

            // Use the url for file from StandardFileHandleMS and read from the file and transform the data into StandardTableData and assign it to Action's crate storage
            //StandardTableDataCM tableDataMS = ExcelUtils.GetTableData(fileHandleMS.DockyardStorageUrl);

            //return tableDataMS;
        }

        /// <summary>
        /// Create configuration controls crate.
        /// </summary>
        private Crate CreateConfigurationControlsCrate(IDictionary<string, string> spreadsheets, string selectedSpreadsheet = null)
        {
            var controlList = new List<ControlDefinitionDTO>();

            var spreadsheetControl = new DropDownList()
            {
                Label = "Select a Google Spreadsheet",
                Name = "select_spreadsheet",
                Required = true,
                Events = new List<ControlEvent>() { ControlEvent.RequestConfig },
                Source = new FieldSourceDTO
                {
                    Label = "Select a Google Spreadsheet",
                    ManifestType = CrateManifestTypes.StandardDesignTimeFields
                },
                ListItems = spreadsheets
                    .Select(pair => new ListItem()
                    {
                        Key = pair.Value,
                        Value = pair.Key,
                        Selected = string.Equals(pair.Key, selectedSpreadsheet, StringComparison.OrdinalIgnoreCase)
                    })
                    .ToList(),
                selectedKey = spreadsheets.FirstOrDefault(a => a.Key == selectedSpreadsheet).Value,
                Value = selectedSpreadsheet
            };
            controlList.Add(spreadsheetControl);

            var textBlockControlField = GenerateTextBlock("",
                "This Action will try to extract a table of rows from the first worksheet in the selected spreadsheet. The rows should have a header row.",
                "well well-lg TextBlockClass");
            controlList.Add(textBlockControlField);
            return PackControlsCrate(controlList.ToArray());
        }

        /// <summary>
        /// Looks for upstream and downstream Creates.
        /// </summary>
        protected override Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {

            //build a controls crate to render the pane
            var authDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
            var spreadsheets = _google.EnumerateSpreadsheetsUris(authDTO);
            var configurationControlsCrate = CreateConfigurationControlsCrate(spreadsheets);

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Replace(AssembleCrateStorage(configurationControlsCrate));
                crateStorage.Add(GetAvailableRunTimeTableCrate("--"));
            }

            return Task.FromResult(curActivityDO);
        }

        /// <summary>
        /// If there's a value in select_file field of the crate, then it is a followup call.
        /// </summary>
        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            var spreadsheetsFromUserSelectionControl = FindControl(CrateManager.GetStorage(curActivityDO.CrateStorage), "select_spreadsheet");

            var hasDesignTimeFields = CrateManager.GetStorage(curActivityDO)
                .Any(x => x.IsOfType<StandardConfigurationControlsCM>());

            if (hasDesignTimeFields && !string.IsNullOrEmpty(spreadsheetsFromUserSelectionControl.Value))
            {
                return ConfigurationRequestType.Followup;
            }

            return ConfigurationRequestType.Initial;
        }

        private Crate GetAvailableRunTimeTableCrate(string descriptionLabel)
        {
            var availableRunTimeCrates = Crate.FromContent("Available Run Time Crates", new CrateDescriptionCM(
                    new CrateDescriptionDTO
                    {
                        ManifestType = MT.StandardTableData.GetEnumDisplayName(),
                        Label = descriptionLabel,
                        ManifestId = (int)MT.StandardTableData,
                        ProducedBy = "Get_Google_Sheet_Data_v1"
                    }), AvailabilityType.RunTime);
            return availableRunTimeCrates;
        }

        //if the user provides a file name, this action attempts to load the excel file and extracts the column headers from the first sheet in the file.
        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            var spreadsheetsFromUserSelection =
                GetControlsManifest(curActivityDO).FindByName("select_spreadsheet").Value;
            //Create label based on the selected by user spreadsheet name
            var selectedSpreadsheetName = GetSelectSpreadsheetName(curActivityDO);
            // Creating configuration control crate with a file picker and textblock
            var authDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
            var spreadsheets = _google.EnumerateSpreadsheetsUris(authDTO);
            var configControlsCrate = CreateConfigurationControlsCrate(spreadsheets, spreadsheetsFromUserSelection);

            // Remove previously created configuration control crate
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.Remove<StandardConfigurationControlsCM>();
                crateStorage.Add(configControlsCrate);
                //Inform donwstream Activities about the availability of the Run Time crates
                crateStorage.RemoveByLabelPrefix("Available Run Time Crates");
                crateStorage.Add(GetAvailableRunTimeTableCrate(TableCrateLabelPrefix + selectedSpreadsheetName));
            }

            if (!string.IsNullOrEmpty(spreadsheetsFromUserSelection))
            {
                return TransformSpreadsheetDataToStandardTableDataCrate(curActivityDO, authTokenDO, spreadsheetsFromUserSelection);
            }
            else
            {
                return curActivityDO;
            }
        }

        private ActivityDO TransformSpreadsheetDataToStandardTableDataCrate(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO, string spreadsheetUri)
        {
            var authDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
            // Fetch column headers in Excel file and assign them to the action's crate storage as Design TIme Fields crate
            var headers = _google.EnumerateColumnHeaders(spreadsheetUri, authDTO);
            if (headers.Any())
            {
                using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
                {
                    const string label = "Spreadsheet Column Headers";
                    crateStorage.RemoveByLabel(label);
                    var curCrateDTO = CrateManager.CreateDesignTimeFieldsCrate(
                                label,
                                headers.Select(col => new FieldDTO() { Key = col.Key, Value = col.Key, Availability = Data.States.AvailabilityType.RunTime }).ToArray()
                            );
                    crateStorage.Add(curCrateDTO);
                }
            }

            CreatePayloadCrate_SpreadsheetRows(curActivityDO, spreadsheetUri, authDTO, headers);

            return curActivityDO;
        }

        private ActivityDO TransformSpreadsheetDataToPayloadDataCrate(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO, string spreadsheetUri)
        {
            var rows = new List<TableRowDTO>();

            var authDTO = JsonConvert.DeserializeObject<GoogleAuthDTO>(authTokenDO.Token);
            var extractedData = _google.EnumerateDataRows(spreadsheetUri, authDTO);
            if (extractedData == null) return curActivityDO;
            var selectedSpreadsheetName = GetSelectSpreadsheetName(curActivityDO);
            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.RemoveByLabelPrefix(TableCrateLabelPrefix);
                crateStorage.Add(CrateManager.CreateStandardTableDataCrate(TableCrateLabelPrefix + selectedSpreadsheetName, false, extractedData.ToArray()));
            }

            return curActivityDO;
        }

        private void CreatePayloadCrate_SpreadsheetRows(ActivityDO curActivityDO, string spreadsheetUri, GoogleAuthDTO authDTO, IDictionary<string, string> headers)
        {
            // To fetch rows in Excel file and assign them to the action's crate storage as Standard Table Data crate. 
            // This functionality is commented due to performance issue. 
            // To fetch rows in excel file, please uncomment below line of code.
            // var rows = _google.EnumerateDataRows(spreadsheetUri, authDTO);

            var rows = new List<TableRowDTO>();
            var headerTableRowDTO = new TableRowDTO() { Row = new List<TableCellDTO>(), };

            foreach (var header in headers)
            {
                var tableCellDTO = TableCellDTO.Create(header.Key, header.Value);
                headerTableRowDTO.Row.Add(tableCellDTO);
            }

            rows.Add(headerTableRowDTO);

            var selectedSpreadsheetName = GetSelectSpreadsheetName(curActivityDO);

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                crateStorage.RemoveByLabelPrefix(TableCrateLabelPrefix);
                crateStorage.Add(CrateManager.CreateStandardTableDataCrate(TableCrateLabelPrefix + selectedSpreadsheetName, false, rows.ToArray()));
            }
        }

        private string GetSelectSpreadsheetName(ActivityDO curActivityDO)
        {
            var dropDownListControl = (DropDownList)GetControlsManifest(curActivityDO).FindByName("select_spreadsheet");
            //get the spreadsheet name
            var spreadsheetName = dropDownListControl.selectedKey;
            return spreadsheetName;
        }
    }
}