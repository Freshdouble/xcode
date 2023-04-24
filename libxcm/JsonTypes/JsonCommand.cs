using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace libxcm.JsonTypes
{
    internal class JsonCommand
    {
        public JsonSymbol[] Fields { get; set; } = new JsonSymbol[0];
        public string Type { get; set; }
        public string Name { get; set; }

        public void FillCommand(Command command)
        {
            if(Type.ToLower() == "sendcommand" && Name.ToLower() == command.Name.ToLower())
            {
                foreach (var symbol in command)
                {
                    if (symbol.IsAnonymous)
                    {

                        foreach (var element in Fields.TakeWhile(x => x.IsAnonymous).SelectMany(x => x.Elements))
                        {
                            var entry = symbol.FindEntry(element.Name);
                            if (entry != null)
                            {
                                element.FillEntry(entry);
                            }
                        }
                    }
                    else
                    {
                        Fields.TakeWhile(x => x.Name.ToLower() == symbol.Name.ToLower()).First()?.FillSymbol(symbol);
                    }
                }
            }
        }
    }
}
