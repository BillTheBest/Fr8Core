using System.Collections.Generic;
using System.Web.Http;
using Data.States;
using Utilities.Configuration.Azure;
using System.Web.Http.Description;
using Fr8Data.DataTransferObjects;
using Fr8Data.Manifests;
using Fr8Data.States;

namespace terminalQuickBooks.Controllers
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
                Name = "terminalQuickBooks",
                Label = "QuickBooks",
                TerminalStatus = TerminalStatus.Active,
                Endpoint = CloudConfigurationManager.GetSetting("terminalQuickBooks.TerminalEndpoint"),
                Version = "1",
                AuthenticationType = AuthenticationType.External
            };
            var webService = new WebServiceDTO()
            {
                Name = "QuickBooks",
                IconPath = "/Content/icons/web_services/quickbooks-icon-64x64.png"
            };
            var createJournalEntryActionTemplate = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Create_Journal_Entry",
                Label = "Create Journal Entry",
                Category = ActivityCategory.Forwarders,
                Terminal = terminal,
                NeedsAuthentication = true,
                MinPaneWidth = 330,
                WebService = webService
            };
            var convertTableDataToAccountingTransaction = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Convert_TableData_To_AccountingTransactions",
                Label = "Convert Table Data To Accounting Transactions",
                Category = ActivityCategory.Processors,
                Terminal = terminal,
                NeedsAuthentication = true,
                MinPaneWidth = 330,
                WebService = webService
            };
            var actionList = new List<ActivityTemplateDTO>()
            {
                createJournalEntryActionTemplate,
                convertTableDataToAccountingTransaction
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