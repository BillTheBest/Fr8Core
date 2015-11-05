﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Constants
{
    public enum MT : int
    {
        [Display(Name = "Standard Design-Time Fields")]
        StandardDesignTimeFields = 3,

        [Display(Name = "Dockyard Plugin Event or Incident Report")]
        EventOrIncidentReport = 2,

        [Display(Name = "Standard Payload Keys")]
        StandardPayloadKeys = 4,

        [Display(Name = "Standard Payload Data")]
        StandardPayloadData = 5,
        
        [Display(Name = "Standard Configuration Controls")]
        StandardConfigurationControls = 6,

        [Display(Name = "Standard Event Report")]
        StandardEventReport = 7,

        [Display(Name = "Standard Event Subscription")]
        StandardEventSubscription = 8,

        [Display(Name = "Standard Table Data")]
        StandardTableData = 9,

        [Display(Name = "Standard File Handle")]
        StandardFileHandle = 10,

        [Display(Name = "Standard Routing Directive")]
        StandardRoutingDirective = 11,

        [Display(Name = "Standard Authentication")]
        StandardAuthentication = 12,

        [Display(Name = "Standard Logging Crate")]
        StandardLoggingCrate = 13,

        [Display(Name = "Logging Data")]
        LoggingData = 10013,

        [Display(Name = "Docusign Event")]
        DocuSignEvent = 14,

        [Display(Name = "Docusign Envelope")]
        DocuSignEnvelope = 15,

        [Display(Name = "Standard Security Crate")]
        StandardSecurityCrate = 16,

        [Display(Name = "Standard Query Crate")]
        StandardQueryCrate = 17,

        [Display(Name = "Standard Email Message")]
        StandardEmailMessage = 18,

        [Display(Name = "Standard Fr8 Routes")]
        StandardFr8Routes = 19,

        [Display(Name = "Standard Fr8 Hubs")]
        StandardFr8Hubs = 20,

        [Display(Name = "Standard Fr8 Containers")]
        StandardFr8Containers = 21,

        [Display(Name = "Standard Parsing Record")]
        StandardParsingRecord = 22,

        [Display(Name = "Standard Fr8 Terminal")]
        StandardFr8Terminal = 23,

        [Display(Name = "Standard File List")]
        StandardFileList = 24,

        [Display(Name = "Docusign Recipient")]
        DocuSignRecipient = 26
    }
}
