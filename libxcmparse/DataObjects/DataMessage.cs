using libxcm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;

namespace libxcmparse.DataObjects
{
    public class DataMessage : Message, IEnumerable<Symbol>
    {
        public event EventHandler Received;
        public static Message MessageFactory(XmlNode messageNode, List<Symbol> knownSymbols)
        {
            return new DataMessage(messageNode, knownSymbols);
        }
        public static Symbol SymbolFactory(XmlNode symbolNode, bool isNested)
        {
            return new DataSymbol(symbolNode, isNested);
        }

        public static Entry EntryFactory(XmlNode entryNode)
        {
            return new DataEntry(entryNode);
        }
        public DataMessage(XmlNode messageNode, List<Symbol> knownSymbols) : base(messageNode, knownSymbols, SymbolFactory, EntryFactory)
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

        public void ParseMessage(IEnumerable<byte> message)
        {
            int offset = 0;
            byte[] msgArray = message as byte[] ?? message.ToArray();
            foreach (var symbol in this)
            {
                if(offset >= msgArray.Length)
                {
                    break;
                }
                if(symbol is DataSymbol datasymbol)
                {
                    byte[] data;
                    if (symbol.IsVariableLength)
                    {
                        data = new byte[message.Count()];
                    }
                    else
                    {
                        data = new byte[symbol.ByteCount];
                    }
                    Array.Copy(msgArray, offset, data, 0, Math.Min(data.Length, msgArray.Length - offset));
                    offset += datasymbol.ParseMessage(data);
                }
            }
            Received?.Invoke(this, new EventArgs());
        }

        public byte[] GetData()
        {
            if (IsVariableLength)
            {
                throw new NotSupportedException("Variable Length commands are not supported");
            }
            byte[] ret = new byte[GetByteLength()];
            int offset = 0;
            if (Identifier != null && Identifier.Length > 0)
            {
                Array.Copy(Identifier, ret, Identifier.Length);
                offset += Identifier.Length;
            }
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
