using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libxcm.JsonTypes
{
    internal class JsonSymbol
    {
        public JsonElement[] Elements { get; set; } = new JsonElement[0];
        public string Name { get; set; }

        public bool IsAnonymous => Name?.ToLower().Contains("anonymous") ?? true;

        public void FillSymbol(Symbol symbol)
        {
            foreach(var entry in symbol)
            {
                Elements.TakeWhile(x => x.Name.ToLower() == entry.Name.ToLower()).First()?.FillEntry(entry);
            }
        }
    }
}
