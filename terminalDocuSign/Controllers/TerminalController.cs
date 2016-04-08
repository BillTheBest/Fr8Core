using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using AutoMapper;
using Newtonsoft.Json;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Services;
using TerminalBase.BaseClasses;
using Utilities.Configuration.Azure;
using System.Web.Http.Description;
using Data.Interfaces.Manifests;

namespace terminalDocuSign.Controllers
{
    [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(StandardFr8TerminalCM))]
        public IHttpActionResult Get()
        {
            var terminal = new TerminalDTO()
            {
                Name = "terminalDocuSign",
                TerminalStatus = TerminalStatus.Active,
                Endpoint = CloudConfigurationManager.GetSetting("terminalDocuSign.TerminalEndpoint"),
                Version = "1",
                AuthenticationType = AuthenticationType.Internal
            };

            var webService = new WebServiceDTO
            {
                Name = "DocuSign",
                IconPath = "/Content/icons/web_services/docusign-icon-64x64.png"
            };


            var process_personal_report = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Process_Personal_Report",
                Label = "Process Personal Report",
                Category = ActivityCategory.Forwarders,
                Terminal = terminal,
                NeedsAuthentication = true,
                MinPaneWidth = 380,
                WebService = webService,
            };

            var waitForDocusignEventActionTemplate = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Monitor_DocuSign_Envelope_Activity",
                Label = "Monitor DocuSign Envelope Activity",
                Category = ActivityCategory.Monitors,
                Terminal = terminal,
                NeedsAuthentication = true,
                MinPaneWidth = 380,
                WebService = webService,
                ShowDocumentation = ActivityResponseDTO.CreateDocumentationResponse("MenuItem", "Monitor_DocuSign_Envelope_Activity_SampleHelp1")
            };

            var sendDocuSignEnvelopeActionTemplate = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Send_DocuSign_Envelope",
                Label = "Send DocuSign Envelope",
                Category = ActivityCategory.Forwarders,
                Tags = "AggressiveReload",
                Terminal = terminal,
                NeedsAuthentication = true,
                WebService = webService,
                MinPaneWidth = 330
            };

            var useDocuSignTemplateWithNewDocument = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Use_DocuSign_Template_With_New_Document",
                Label = "Use DocuSign Template With New Document",
                Category = ActivityCategory.Forwarders,
                Tags = "AggressiveReload",
                Terminal = terminal,
                NeedsAuthentication = true,
                WebService = webService,
                MinPaneWidth = 380
            };

            var extractDataFromEnvelopeActionTemplate = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Get_DocuSign_Envelope",
                Label = "Get DocuSign Envelope",
                Category = ActivityCategory.Receivers,
                Terminal = terminal,
                NeedsAuthentication = true,
                WebService = webService,
                MinPaneWidth = 330
            };

            var getDocuSignTemplateActionTemplate = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Get_DocuSign_Template",
                Label = "Get DocuSign Template",
                Category = ActivityCategory.Receivers,
                Terminal = terminal,
                NeedsAuthentication = true,
                WebService = webService,
                MinPaneWidth = 330
            };

            var recordDocuSignEvents = new ActivityTemplateDTO
            {
                Name = "Prepare_DocuSign_Events_For_Storage",
                Label = "Prepare DocuSign Events For Storage",
                Version = "1",
                Category = ActivityCategory.Forwarders,
                Terminal = terminal,
                NeedsAuthentication = true,
                WebService = webService,
                MinPaneWidth = 330,
                Tags = "internal"
            };

            var mailMergeActionTemplate = new ActivityTemplateDTO
            {
                Name = "Mail_Merge_Into_DocuSign",
                Label = "Mail Merge Into DocuSign",
                Version = "1",
                NeedsAuthentication = true,
                Category = ActivityCategory.Solution,
                Terminal = terminal,
                WebService = webService,
                MinPaneWidth = 500,
                Tags = "UsesReconfigureList"
            };

            var collectFormDataSolution = new ActivityTemplateDTO
            {
                Name = "Extract_Data_From_Envelopes",
                Label = "Extract Data From Envelopes",
                Version = "1",
                Category = ActivityCategory.Solution,
                Terminal = terminal,
                WebService = webService,
                MinPaneWidth = 380,
                NeedsAuthentication = true
            };

            var trackDocuSignRecipientsSolution = new ActivityTemplateDTO
            {
                Name = "Track_DocuSign_Recipients",
                Label = "Track DocuSign Recipients",
                Version = "1",
                Category = ActivityCategory.Solution,
                NeedsAuthentication = true,
                Terminal = terminal,
                WebService = webService,
                MinPaneWidth = 380
            };


            var queryDocusign = new ActivityTemplateDTO
            {
                Name = "Query_DocuSign",
                Label = "Query DocuSign",
                Version = "1",
                Category = ActivityCategory.Receivers,
                NeedsAuthentication = true,
                Terminal = terminal,
                WebService = webService,
                MinPaneWidth = 380
            };

            var generateDocusignReport = new ActivityTemplateDTO
            {
                Name = "Generate_DocuSign_Report",
                Label = "Generate a DocuSign Report",
                Version = "1",
                Category = ActivityCategory.Solution,
                NeedsAuthentication = true,
                Terminal = terminal,
                WebService = webService,
                MinPaneWidth = 420,
                Tags = "HideChildren"
            };

            var searchDocusignHistory = new ActivityTemplateDTO
            {
                Name = "Search_DocuSign_History",
                Label = "Search DocuSign History",
                Version = "1",
                Category = ActivityCategory.Receivers,
                NeedsAuthentication = true,
                Terminal = terminal,
                WebService = webService,
                MinPaneWidth = 380,
                Tags = "internal"
            };

            var actionList = new List<ActivityTemplateDTO>()
            {
                waitForDocusignEventActionTemplate,
                extractDataFromEnvelopeActionTemplate,
                sendDocuSignEnvelopeActionTemplate,
                useDocuSignTemplateWithNewDocument,
                recordDocuSignEvents,
                mailMergeActionTemplate,
                collectFormDataSolution,
                trackDocuSignRecipientsSolution,
                queryDocusign,
                generateDocusignReport,
                searchDocusignHistory,
                getDocuSignTemplateActionTemplate,
                process_personal_report
            };

            var curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = terminal,
                Activities = actionList
            };

            return Json(curStandardFr8TerminalCM);
        }
    }
}