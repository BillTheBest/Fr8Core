﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Data.Control;
using Data.Crates;
using Newtonsoft.Json;
using StructureMap;
using Data.Interfaces;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Exceptions;
using Hub.Interfaces;
using Hub.Managers;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using terminalExcel.Infrastructure;

namespace terminalExcel.Actions
{
    public class Load_Table_Data_v1 : BaseTerminalAction
    {
        private class ActionUi : StandardConfigurationControlsCM
        {
            [JsonIgnore]
            public readonly ControlDefinitionDTO select_file;
            
            public ActionUi(bool includeTextBlockControl)
            {
                Controls = new List<ControlDefinitionDTO>();

                Controls.Add((select_file = new ControlDefinitionDTO(ControlTypes.FilePicker)
                {
                    Label = "Select an Excel file",
                    Name = "select_file",
                    Required = true,
                    Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                },
                    Source = new FieldSourceDTO
                    {
                        Label = "Select an Excel file",
                        ManifestType = CrateManifestTypes.StandardConfigurationControls
                    },
                }));

                Controls.Add(new TextBlock
                {
                    Label = "",
                    Value = "This Action will try to extract a table of rows from the first spreadsheet in the file. The rows should have a header row.",
                    CssClass = "well well-lg TextBlockClass"
                });
                
                if (includeTextBlockControl)
                {
                    Controls.Add(new TextBlock
                    {
                        Label = "",
                        Value = "File successfully uploaded.",
                        CssClass = "well well-lg"
                    });
                }
            }
        }


        /// <summary>
        /// Action processing infrastructure.
        /// </summary>
        public async Task<PayloadDTO> Run(ActionDO curActionDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            return await CreateStandardPayloadDataFromStandardTableData(curActionDO, containerId);
        }

        private async Task<PayloadDTO> CreateStandardPayloadDataFromStandardTableData(ActionDO curActionDO, Guid containerId)
        {
            var processPayload = await GetProcessPayload(curActionDO, containerId);

            var tableDataMS = await GetTargetTableData(
                curActionDO,
                Crate.GetStorage(curActionDO)
            );

            if (!tableDataMS.FirstRowHeaders)
            {
                throw new Exception("No headers found in the Standard Table Data Manifest.");
            }

            // Create a crate of payload data by using Standard Table Data manifest and use its contents to tranform into a Payload Data manifest.
            // Add a crate of PayloadData to action's crate storage
            
            
            using (var updater = Crate.UpdateStorage(processPayload))
            {
                updater.CrateStorage.Add(Crate.CreatePayloadDataCrate("ExcelTableRow", "Excel Data", tableDataMS));
            }
            return processPayload;            
        }

        private async Task<StandardTableDataCM> GetTargetTableData(ActionDO actionDO, CrateStorage curCrateStorageDTO)
        {
            // Find crates of manifest type Standard Table Data
            var standardTableDataCrates = curCrateStorageDTO.CratesOfType<StandardTableDataCM>();

            // If no crate of manifest type "Standard Table Data" found, try to find upstream for a crate of type "Standard File Handle"
            if (!standardTableDataCrates.Any())
            {
                return await GetUpstreamTableData(actionDO);
            }

            return standardTableDataCrates.First().Content;
        }

        private async Task<StandardTableDataCM> GetUpstreamTableData(ActionDO actionDO)
        {
            var upstreamFileHandleCrates = await GetUpstreamFileHandleCrates(actionDO);

            //if no "Standard File Handle" crate found then return
            if (!upstreamFileHandleCrates.Any())
                return null;

            //if more than one "Standard File Handle" crates found then throw an exception
            if (upstreamFileHandleCrates.Count > 1)
                throw new Exception("More than one Standard File Handle crates found upstream.");

            // Deserialize the Standard File Handle crate to StandardFileHandleMS object
            StandardFileHandleMS fileHandleMS = upstreamFileHandleCrates.First().Get<StandardFileHandleMS>();

            // Use the url for file from StandardFileHandleMS and read from the file and transform the data into StandardTableData and assign it to Action's crate storage
            StandardTableDataCM tableDataMS = ExcelUtils.GetTableData(fileHandleMS.DockyardStorageUrl);

            return tableDataMS;
        }

        //private async Task<FileDO> ProcessFile(string dockyardStorageUrl, string fileName)
        //{
        //    using (var client = new HttpClient())
        //    {
        //        var values = new Dictionary<string, string>
        //                {
        //                    { "DockyardStorageURL", dockyardStorageUrl },
        //                    { "Filename", fileName },
        //                    //{ "AuthorizationToken", curFileDO.DockyardAccountID } //ignoring for now
        //                };

        //        var content = new FormUrlEncodedContent(values);

        //        var response = await client.PostAsync(ConfigurationManager.AppSettings["FileServerApiUrl"], content);

        //        var curFileDOTask = await response.Content.ReadAsAsync<FileDO>();

        //        return curFileDOTask;
        //    }
        //}


        /// <summary>
        /// Looks for upstream and downstream Creates.
        /// </summary>
        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            if (curActionDO.Id != Guid.Empty)
            {

                //Pack the merged fields into a new crate that can be used to populate the dropdownlistbox
                Crate upstreamFieldsCrate = await MergeUpstreamFields(curActionDO, "Select Excel File");

                using (var updater = Crate.UpdateStorage(curActionDO))
                {
                    updater.CrateStorage.Clear();
                    updater.CrateStorage.Add(upstreamFieldsCrate);
                    updater.CrateStorage.Add(PackControls(new ActionUi(false)));
            }
            }
            else
            {
                throw new ArgumentException("Configuration requires the submission of an Action that has a real ActionId");
            }
            return curActionDO;
        }

        /// <summary>
        /// If there's a value in select_file field of the crate, then it is a followup call.
        /// </summary>
        public override ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            var storage = Crate.GetStorage(curActionDO);

            var filePathsFromUserSelection = storage.CrateContentsOfType<StandardConfigurationControlsCM>()
                .Where(x =>
                {
                    var ui = new ActionUi(false);
                    ui.ClonePropertiesFrom(x);
                    return !string.IsNullOrEmpty(ui.select_file.Value);
                }).ToArray();

            var hasDesignTimeFields = storage.CratesOfType<StandardDesignTimeFieldsCM>().Any(x => x.Label == "Spreadsheet Column Headers");

            if (filePathsFromUserSelection.Length == 1 || hasDesignTimeFields)
            {
                return ConfigurationRequestType.Followup;
            }

            return ConfigurationRequestType.Initial;
        }

        //if the user provides a file name, this action attempts to load the excel file and extracts the column headers from the first sheet in the file.
        protected override async Task<ActionDO> FollowupConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            var storage = Crate.GetStorage(curActionDO);
            var filePathsFromUserSelection = storage.CrateContentsOfType<StandardConfigurationControlsCM>()
                .Select(x =>
                {
                    var ui = new ActionUi(false);
                    ui.ClonePropertiesFrom(x);
                    return ui;
                })
                 .Where(x => !string.IsNullOrEmpty(x.select_file.Value)).ToArray();

            if (filePathsFromUserSelection.Length > 1)
            {
                throw new AmbiguityException();
            }

            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                updater.CrateStorage.Remove<StandardConfigurationControlsCM>();
                updater.CrateStorage.Add(PackControls(new ActionUi(true)));

            if (filePathsFromUserSelection.Length > 0)
            {
                    var selectedFilePath = filePathsFromUserSelection[0].select_file.Value;
                    TransformExcelFileDataToStandardTableDataCrate(storage, selectedFilePath);
            }

                return curActionDO;
            }
        }

        private void TransformExcelFileDataToStandardTableDataCrate(CrateStorage storage, string selectedFilePath)
        {
            // Check if the file is an Excel file.
            string ext = Path.GetExtension(selectedFilePath);
            if (ext != ".xls" && ext != ".xlsx")
                throw new ArgumentException("Expected '.xls' or '.xlsx'", "selectedFile");

            FileDO curFileDO = new FileDO()
            {
                CloudStorageUrl = selectedFilePath,
            };

            IFile file = ObjectFactory.GetInstance<IFile>();

            // Read file from repository
            var fileAsByteArray = file.Retrieve(curFileDO);

            // Fetch column headers in Excel file and assign them to the action's crate storage as Design TIme Fields crate
            var headersArray = ExcelUtils.GetColumnHeaders(fileAsByteArray, ext);
            if (headersArray != null)
            {
                var headers = headersArray.ToList();
                var curCrateDTO = Crate.CreateDesignTimeFieldsCrate(
                            "Spreadsheet Column Headers",
                            headers.Select(col => new FieldDTO() { Key = col, Value = col }).ToArray()
                        );

                storage.RemoveByLabel("Spreadsheet Column Headers");
                storage.Add(curCrateDTO);
            }

            CreatePayloadCrate_ExcelRows(storage, fileAsByteArray, headersArray, ext);
        }

        private void CreatePayloadCrate_ExcelRows(CrateStorage storage, byte[] fileAsByteArray, string[] headersArray, string extension)
        {
            // Fetch rows in Excel file and assign them to the action's crate storage as Standard Table Data crate
            var rowsDictionary = ExcelUtils.GetTabularData(fileAsByteArray, extension);
            if (rowsDictionary != null && rowsDictionary.Count > 0)
            {
                var rows = ExcelUtils.CreateTableCellPayloadObjects(rowsDictionary, headersArray);
                if (rows != null && rows.Count > 0)
                {
                    storage.Add(Crate.CreateStandardTableDataCrate("Excel Payload Rows", true, rows.ToArray()));
                }
            }
        }
    }

    // For backward compatibility
    public class Extract_Data_v1 : Load_Table_Data_v1 { }
}