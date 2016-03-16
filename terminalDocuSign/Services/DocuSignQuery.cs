using System;
using System.Collections.Generic;
using Data.Interfaces.DataTransferObjects;

namespace terminalDocuSign.Services
{
    public class DocusignQuery
    {
        public static readonly FieldDTO[] Statuses =
        {
            new FieldDTO("Any status", "<any>"),
            new FieldDTO("Sent", "sent"),
            new FieldDTO("Delivered", "delivered"),
            new FieldDTO("Signed", "signed"),
            new FieldDTO("Completed", "completed"),
            new FieldDTO("Declined", "declined"),
            new FieldDTO("Voided", "voided"),
            new FieldDTO("Timed Out", "timedout"),
            new FieldDTO("Authoritative Copy", "authoritativecopy"),
            new FieldDTO("Transfer Completed", "transfercompleted"),
            new FieldDTO("Template", "template"),
            new FieldDTO("Correct", "correct"),
            new FieldDTO("Created", "created"),
            new FieldDTO("Delivered", "delivered"),
            new FieldDTO("Signed", "signed"),
            new FieldDTO("Declined", "declined"),
            new FieldDTO("Completed", "completed"),
            new FieldDTO("Fax Pending", "faxpending"),
            new FieldDTO("Auto Responded", "autoresponded"),
        };

        public DocusignQuery()
        {
            Conditions = new List<FilterConditionDTO>();
        }

        public DateTime? FromDate;
        public DateTime? ToDate;

        public string SearchText;
        public string Status;
        public string Folder;

        public List<FilterConditionDTO> Conditions { get; set; }
    }
}