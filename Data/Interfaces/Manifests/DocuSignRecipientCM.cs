﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces.Manifests
{
    public class DocuSignRecipientCM : Manifest
    {
        public string Object { get; set; }
        public string Status { get; set; }
        public string DocuSignAccountId { get; set; }
        public string RecipientId { get; set; }
        public DocuSignRecipientCM()
            : base(Constants.MT.DocuSignRecipient)
        {

        }
    }
}
