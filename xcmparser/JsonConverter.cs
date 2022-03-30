using libxcm;
using libxcmparse.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace xcmparser
{
    static class JsonConverter
    {
        private static object SymbolToTagedObject(DataSymbol symb)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>();
            foreach (DataEntry entry in symb)
            {
                var val = EntryToExtendedJSON(entry);
                string name = entry.Name;
                /*
                switch (val)
                {
                    case bool:
                        name += "_bool";
                        break;
                    case string:
                        name += "_string";
                        break;
                    default:
                        break;
                }*/
                tags.Add(name, val);
            }
            return tags;
        }

        public static byte[] ConvertDataToJSONByte(DataMessage msg)
        {
            return Encoding.UTF8.GetBytes(ConvertDataToJSON(msg));
        }

        public static object EntryToExtendedJSON(DataEntry entry)
        {
            return new
            {
                name = entry.Name,
                value = entry.GetValue<object>(),
                isEntry = true,
                isValid = entry.IsValid,
                hasWarning = entry.HasWarning,
            };
        }
        public static string ConvertDataToJSON(DataMessage msg, bool pretty = false)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>();
            foreach (DataSymbol symbol in msg)
            {
                string name = symbol.Name;
                //int counter = 0;
                if (string.IsNullOrWhiteSpace(name))
                {
                    /*
                    do
                    {
                        name = "Anonymous" + counter;
                        counter++;
                    }while(tags.ContainsKey(name));*/
                    foreach (DataEntry entry in symbol)
                    {
                        tags.Add(entry.Name, EntryToExtendedJSON(entry));
                    }
                }
                else
                {
                    tags.Add(name, SymbolToTagedObject(symbol));
                }
            }
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new BigIntegerConverter());
            options.WriteIndented = pretty;
            return JsonSerializer.Serialize(new
            {
                Type = "receiveddata",
                MessageName = msg.Name,
                Fields = tags,
                isExtended = true
            }, options);
        }
    }
}
