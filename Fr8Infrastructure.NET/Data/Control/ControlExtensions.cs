﻿using System.Collections.Generic;
using System.Linq;
using fr8.Infrastructure.Data.DataTransferObjects;

namespace fr8.Infrastructure.Data.Control
{
    public static class DataExtensions
    {
        public static IEnumerable<ListItem> ToListItems(this IEnumerable<FieldDTO> fields)
        {
            return fields.Select(x => new ListItem() { Key = x.Key, Value = x.Value });
        }
    }
}
