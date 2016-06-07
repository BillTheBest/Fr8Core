﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fr8.Infrastructure.Data.Constants;

namespace fr8.Infrastructure.Data.Manifests
{
    public class DocuSignRecipientCM : Manifest
    {
        public string Object { get; set; }
        public string Status { get; set; }
        public string DocuSignAccountId { get; set; }
        public string RecipientId { get; set; }
        public string RecipientEmail { get; set; }
        [MtPrimaryKey]
        public string EnvelopeId { get; set; }
        public DocuSignRecipientCM()
            : base(MT.DocuSignRecipient)
        {

        }
    }
}
