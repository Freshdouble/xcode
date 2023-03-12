using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace libxcm
{
    public class Command : Message
    {
        public Command(XmlNode commandNode, List<Symbol> knownSymbols, Connection inbound, Connection outbound) : base(commandNode, knownSymbols, inbound, outbound)
        {
        }

        protected Command(XmlNode commandNode, List<Symbol> knownSymbols, Func<XmlNode, bool, Symbol> symbolFactory, Func<XmlNode, Entry> EntryFactory, Connection inbound, Connection outbound) : base(commandNode, knownSymbols, symbolFactory, EntryFactory, inbound, outbound)
        {
        }
    }
}
