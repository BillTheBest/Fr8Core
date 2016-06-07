﻿using System.Collections.Generic;
using fr8.Infrastructure.Data.Constants;
using fr8.Infrastructure.Data.DataTransferObjects;

namespace fr8.Infrastructure.Data.Manifests
{
    public class ExternalObjectHandlesCM : Manifest
    {
        public ExternalObjectHandlesCM()
            : base(MT.ExternalObjectHandles)
        {
        }

        public ExternalObjectHandlesCM(IEnumerable<ExternalObjectHandleDTO> handles) : this()
        {
            Handles = new List<ExternalObjectHandleDTO>(handles);
        }

        public ExternalObjectHandlesCM(params ExternalObjectHandleDTO[] handles) : this()
        {
            Handles = new List<ExternalObjectHandleDTO>(handles);
        }


        public List<ExternalObjectHandleDTO> Handles { get; set; }
    }
}
