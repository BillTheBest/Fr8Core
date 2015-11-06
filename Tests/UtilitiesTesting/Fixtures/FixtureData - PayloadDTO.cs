﻿﻿using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
﻿using Data.Crates;
﻿using Hub.Managers;
﻿using StructureMap;

namespace UtilitiesTesting.Fixtures
{
    partial class FixtureData
    {
        public static PayloadDTO PayloadDTO1()
        {
            List<FieldDTO> curFields = new List<FieldDTO>() { new FieldDTO() { Key = "EnvelopeId", Value = "EnvelopeIdValue" } };

            EventReportCM curEventReportMS = new EventReportCM();
            curEventReportMS.EventNames = "DocuSign Envelope Sent";
            curEventReportMS.EventPayload.Add(Crate.FromContent("Standard Event Report", new StandardPayloadDataCM(curFields)));
            var payload = new PayloadDTO(1);

            using (var updater = ObjectFactory.GetInstance<ICrateManager>().UpdateStorage(payload))
            {
                updater.CrateStorage.Add(Crate.FromContent("Standard Event Report", curEventReportMS));
            }

            return payload;
        }

        public static PayloadDTO PayloadDTO2()
        {
            var standardPayload = new StandardPayloadDataCM(new List<FieldDTO>() {new FieldDTO() {Key = "EnvelopeId", Value = "EnvelopeIdValue"}});

            var payload = new PayloadDTO(49);

            using (var updater = ObjectFactory.GetInstance<ICrateManager>().UpdateStorage(payload))
            {
                updater.CrateStorage.Add(Crate.FromContent("Standard Payload Data", standardPayload));
            }
            
            return payload;
        }

    }
}