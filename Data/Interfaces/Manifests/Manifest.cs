﻿using Data.Constants;
using Data.Interfaces.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Data.Interfaces.Manifests
{
    public class Manifest
    {
        public MT ManifestType { get; private set; }
        public int ManifestId
        {
            get { return (int)ManifestType; }
        }
        public string ManifestName
        {
            get { return ManifestType.GetEnumDisplayName(); }
        }

        public Manifest()
        {
        }

        public Manifest(MT manifestType)
        {
            ManifestType = manifestType;
        }

        // Using reflection fetching the names of property.
        public List<FieldDTO> GetProperties(Type curType)
        {
            var curProperties = curType.GetProperties();
            var curPropertiesList = new List<FieldDTO>();

            foreach (var item in curProperties)
            {
                curPropertiesList.Add(new FieldDTO()
                {
                    Key = item.Name,
                    Value = item.PropertyType.Name,
                });
            }
            return curPropertiesList;
        }

    }
}
