using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace CloudMosaic.API.Models
{
    public class EnumPropertyConverter : IPropertyConverter
    {
        public object FromEntry(DynamoDBEntry entry)
        {
            if (entry == null || entry is DynamoDBNull)
                return 0;

            return (int)entry;
        }

        public DynamoDBEntry ToEntry(object value)
        {
            var intValue = Convert.ToInt32(value);
            if(intValue == 0)
            {
                return new Primitive()
                {
                    Type = DynamoDBEntryType.Numeric,
                    Value = string.Empty
                };
            }
            return (Primitive)intValue;
        }
    }
}
