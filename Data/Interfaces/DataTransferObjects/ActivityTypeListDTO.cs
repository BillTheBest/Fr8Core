﻿using Newtonsoft.Json;

namespace Data.Interfaces.DataTransferObjects
{
    public class ActivityTypeListDTO
    {
        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}