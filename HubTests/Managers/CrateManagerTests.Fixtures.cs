﻿using System.Collections.Generic;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json.Linq;

namespace HubTests.Managers
{
    partial class CrateManagerTests
    {
        private static CrateStorageDTO GetKnownManifestsStorageDto(string key = "value")
        {
            var storage = new CrateStorageDTO
            {
                Crates = new[]
                {
                    TestKnownCrateDto("id1", key + "1"),
                    TestKnownCrateDto("id2", key + "2"),
                }
            };

            return storage;
        }
        
        private static CrateStorageDTO GetUnknownManifestsStorageDto()
        {
            var storage = new CrateStorageDTO
            {
                Crates = new[]
                {
                    TestUnknownCrateDto("id1", "value1"),
                    TestUnknownCrateDto("id2", "value2"),
                }
            };

            return storage;
        }



        private static CrateDTO TestUnknownCrateDto(string id, string value)
        {
            return new CrateDTO
            {
                Label = id + "_label",
                Id = id,
                ManifestId = 888888,
                ManifestType = "Unknwon manifest",
                Contents = value
            };
        }

        private static CrateDTO TestKnownCrateDto(string id, string value)
        {
            var manifest = TestManifest(value);

            return new CrateDTO
            {
                Label = id + "_label",
                Id = id,
                ManifestId = manifest.ManifestType.Id,
                ManifestType = manifest.ManifestType.Type,
                Contents = JToken.FromObject(manifest)
            };
        }

        private static StandardDesignTimeFieldsCM TestManifest(string value = "value")
        {
            return new StandardDesignTimeFieldsCM
            {
                Fields = new List<FieldDTO>
                {
                    new FieldDTO("key", value)
                }
            };
        }
    }
}
