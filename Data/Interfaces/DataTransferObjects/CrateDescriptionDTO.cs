﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data.Interfaces.DataTransferObjects
{
    public class CrateDescriptionDTO
    {
        [JsonProperty("manifestId")]
        public int ManifestId { get; set; }

        [JsonProperty("manifestType")]
        public string ManifestType{ get; set; }

        [JsonProperty("label")]
        public string Label{ get; set; }

        [JsonProperty("producedBy")]
        public string ProducedBy { get; set; }

        [JsonProperty("selected")]
        public bool Selected { get; set; }

        [JsonProperty("fields")]
        public List<FieldDTO> Fields { get; set; }

        public CrateDescriptionDTO()
        {
            Fields = new List<FieldDTO>();
            Selected = false;
        }
    }
}
