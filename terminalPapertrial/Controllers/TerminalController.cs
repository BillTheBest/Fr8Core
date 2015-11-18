using System.Collections.Generic;
using System.Web.Http;
using Data.Entities;
using Data.States;
using Utilities.Configuration.Azure;
using System.Web.Http.Description;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;

namespace terminalPapertrail.Controllers
{
    [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof (StandardFr8TerminalCM))]
        public IHttpActionResult Get()
        {
            var terminal = new TerminalDTO()
            {
                Name = "terminalPapertrail",
                TerminalStatus = TerminalStatus.Active,
                Endpoint = CloudConfigurationManager.GetSetting("TerminalEndpoint"),
                Version = "1"
            };

            var webService = new WebServiceDO
            {
                Name = "Papertrail",
                IconPath = "/Content/icons/web_services/papertrail-icon-64x64.png"
            };

            var writeToLogActionTemplate = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Write_To_Log",
                Label = "Write To Log",
                Category = ActivityCategory.Processors.ToString(),
                Terminal = terminal,
                AuthenticationType = AuthenticationType.None,
                MinPaneWidth = 330,
                WebServiceName = webService.Name
            };

            var curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = terminal,
                Actions = new List<ActivityTemplateDTO> {writeToLogActionTemplate}
            };

            return Json(curStandardFr8TerminalCM);
        }
    }
}