﻿using libxcm;
using libxcm.JsonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace xcmparser
{
    public class JsonConverter : IMessageConverter
    {
        private static object SymbolToTagedObject(Symbol symb)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>();
            foreach (Entry entry in symb)
            {
                var val = EntryToExtendedJSON(entry);
                string name = entry.Name;
                tags.Add(name, val);
            }
            return new
            {
                value = tags,
                isEntry = false,
                isSymbol = true
            };
        }

        public static byte[] ConvertDataToJSONByte(Message msg)
        {
            return Encoding.UTF8.GetBytes(ConvertDataToJSON(msg));
        }

        public static object EntryToExtendedJSON(Entry entry)
        {
            return new
            {
                value = entry.GetValue<object>()
            };
        }
        public static string ConvertDataToJSON(Message msg, bool pretty = false)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>();
            foreach (Symbol symbol in msg)
            {
                string name = symbol.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    foreach (Entry entry in symbol)
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
            }, options) + "\n";
        }

        public byte[] ConvertToByteArray(Message msg)
        {
            return ConvertDataToJSONByte(msg);
        }

        public static void GetDataFromJson(IEnumerable<byte>data, Command com)
        {
            GetDataFromJson(data as byte[] ?? data.ToArray(), com);
        }

        public static void GetDataFromJson(byte[] data, Command com)
        {
            var str = Encoding.UTF8.GetString(data);
            GetDataFromJson(str, com);
        }

        public static void GetDataFromJson(string json, Command com)
        {
            var jsoncommand = JsonSerializer.Deserialize<JsonCommand>(json);
            jsoncommand?.FillCommand(com);
        }
    }
}
