using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data.Interfaces.DataTransferObjects
{
    public class AvailableDataDTO
    {
        [JsonProperty("availableFields")]
        public readonly List<FieldDTO> AvailableFields = new List<FieldDTO>();

        [JsonProperty("availableCrates")]
        public readonly List<CrateDescriptionDTO> AvailableCrates = new List<CrateDescriptionDTO>();
    }
}