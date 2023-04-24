using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libxcm.JsonTypes
{
    internal class JsonElement
    {
        public object value { get; set; } = null;
        public string Name { get; set; }

        public void FillEntry(Entry entry)
        {
            entry.Value.SetValue(((System.Text.Json.JsonElement)value).GetRawText());
        }
    }
}
