﻿using libxcm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace libxcmparse.DataObjects
{
    public class DataCommand : Command
    {
        public DataCommand(XmlNode commandNode, List<Symbol> knownSymbols, Connection inbound, Connection outbound) : this(commandNode, knownSymbols, DataMessage.SymbolFactory, DataMessage.EntryFactory, inbound, outbound)
        {
        }

        protected DataCommand(XmlNode commandNode, List<Symbol> knownSymbols, Func<XmlNode, bool, Symbol> symbolFactory, Func<XmlNode, Entry> EntryFactory, Connection inbound, Connection outbound) : base(commandNode, knownSymbols, symbolFactory, EntryFactory, inbound, outbound)
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
