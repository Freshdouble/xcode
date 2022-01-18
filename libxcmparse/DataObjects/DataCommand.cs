using libxcm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace libxcmparse.DataObjects
{
    public class DataCommand : Command
    {
        public static Command CommandFactory(XmlNode messageNode, List<Symbol> knownSymbols)
        {
            return new DataCommand(messageNode, knownSymbols);
        }
        public DataCommand(XmlNode commandNode, List<Symbol> knownSymbols) : this(commandNode, knownSymbols, DataMessage.SymbolFactory, DataMessage.EntryFactory)
        {
        }

        protected DataCommand(XmlNode commandNode, List<Symbol> knownSymbols, Func<XmlNode, bool, Symbol> symbolFactory, Func<XmlNode, Entry> EntryFactory) : base(commandNode, knownSymbols, symbolFactory, EntryFactory)
        {
            List<DataSymbol> sibblings = new List<DataSymbol>();
            foreach (DataSymbol symb in this)
            {
                sibblings.Add(symb);
            }
            foreach (DataSymbol symb in this)
            {
                symb.Sibblings = sibblings;
            }
        }

        public byte[] GetData()
        {
            if(IsVariableLength)
            {
                throw new NotSupportedException("Variable Length commands are not supported");
            }
            byte[] ret = new byte[GetByteLength()];
            int offset = 0;
            Array.Copy(Identifier, ret, Identifier.Length);
            offset += Identifier.Length;
            foreach (DataSymbol symbol in this)
            {
                int bytelength = symbol.ByteCount;
                Array.Copy(symbol.GetData(), 0, ret, offset, bytelength);
                offset += bytelength;
            }
            return ret;
        }
    }
}
