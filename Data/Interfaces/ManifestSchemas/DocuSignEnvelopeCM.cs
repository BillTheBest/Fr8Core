﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces.ManifestSchemas
{
    public class DocuSignEnvelopeCM : Manifest
    {
        public string Status { get; set; }
        public string CreateDate { get; set; }
        public string SentDate { get; set; }
        public string DeliveredDate { get; set; }
        public string CompletedDate { get; set; }
        public string EnvelopeId { get; set; }
        public string ExternalAccountId { get; set; }

        public DocuSignEnvelopeCM()
            : base(Constants.MT.DocuSignEnvelope)
        {

        }
    }
}