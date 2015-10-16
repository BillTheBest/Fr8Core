﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using DocuSign.Integrations.Client;
using pluginDocuSign.DataTransferObjects;
using pluginDocuSign.Services;

namespace pluginDocuSign.Interfaces
{
	public interface IDocuSignTemplate
	{
		List<string> GetMappableSourceFields(DocuSignEnvelope envelope);
		IEnumerable<string> GetMappableSourceFields(string templateId);
		IEnumerable<DocuSignTemplateDTO> GetTemplates(Fr8AccountDO curDockyardAccount);
        IEnumerable<DocuSignTemplateDTO> GetTemplates(string email, string apiPassword);
		List<string> GetUserFields(DocuSignTemplateDTO curDocuSignTemplateDTO);
		DocuSignTemplateDTO GetTemplateById(string templateId);
	}
}
