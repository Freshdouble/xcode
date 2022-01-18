using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace libxcm
{
    public class Message : IEnumerable<Symbol>
    {
        protected readonly byte[] identifier;
        protected readonly List<Symbol> symbols = new List<Symbol>();

        public int IDOffset { get; protected set; } = 0;
        private int idlength = -1;

        private static Symbol SymbolFactory(XmlNode symbolNode, bool isNested)
        {
            return new Symbol(symbolNode, isNested);
        }

        private static Entry EntryFactory(XmlNode entryNode)
        {
            return new Entry(entryNode);
        }

        public Message(XmlNode messageNode, List<Symbol> knownSymbols) : this(messageNode, knownSymbols, SymbolFactory, EntryFactory)
        {
            
        }

        protected Message(XmlNode messageNode, List<Symbol> knownSymbols, Func<XmlNode, bool, Symbol> symbolFactory, Func<XmlNode, Entry> EntryFactory)
        {
            bool isString;
            identifier = GetIdentifierData(messageNode, out isString);
            IdentifierIsString = isString;
            Name = messageNode.Attributes["name"].Value;
            if(messageNode.Attributes["idoffset"] != null)
            {
                IDOffset = int.Parse(messageNode.Attributes["idoffset"].Value);
            }
            if (messageNode.Attributes["idlength"] != null)
            {
                idlength = int.Parse(messageNode.Attributes["idlength"].Value);
            }
            using (MemoryStream str = new MemoryStream())
            {
                var sw = new StreamWriter(str);
                foreach (XmlNode symbolNode in messageNode)
                {
                    if (symbolNode.NodeType == XmlNodeType.Element)
                    {
                        if (symbolNode.Attributes["ref"] != null)
                        {
                            string symbolRef = symbolNode.Attributes["ref"].Value;
                            Symbol knownSymbol = (knownSymbols.Find(x => x.Name == symbolRef) ?? throw new ArgumentException("ref doesn't point to a defined symbol")).Copy();
                            if (symbolNode.Attributes["name"] != null)
                            {
                                knownSymbol.Name = symbolNode.Attributes["name"].Value;
                            }
                            foreach (XmlNode entryNode in symbolNode)
                            {
                                if (entryNode.NodeType != XmlNodeType.Comment)
                                {
                                    knownSymbol.Add(EntryFactory(entryNode));
                                }
                            }
                            knownSymbol.IsGlobal = true;
                            symbols.Add(knownSymbol);
                        }
                        else
                        {
                            var symb = symbolFactory(symbolNode, true);
                            symb.IsGlobal = false;
                            symbols.Add(symb);
                        }
                    }
                    else if (symbolNode.NodeType == XmlNodeType.Comment)
                    {
                        sw.WriteLine(symbolNode.Value);
                    }
                }
                sw.Flush();
                str.Position = 0;
                DocuText.Add((new StreamReader(str)).ReadToEnd());
            }
        }

        public List<string> DocuText { get; set; } = new List<string>();

        public bool IdentifierIsString { get; protected set; } = false;

        public byte[] Identifier => identifier;

        public int IDByteLength => idlength < 1 ? Identifier.Length : idlength;

        public string Name { get; set; }

        public string GetIDString()
        {
            return Encoding.ASCII.GetString(identifier);
        }

        public static bool IsHex(string hex)
        {
            return hex.StartsWith("0x");
        }

        public static byte[] GetHex(string hex)
        {
            List<byte> id = new List<byte>();
            if (!IsHex(hex))
                throw new FormatException("The string must be a valid hex representation");
            string hexPart = hex.Substring(2);
            if(hexPart.Length % 2 != 0)
            {
                hexPart = "0" + hexPart; //Fill string with leading zero
            }
            string splitString = string.Join(string.Empty, hexPart.Select((x, i) => i > 0 && i % 2 == 0 ? string.Format(" {0}", x) : x.ToString())); //Insert blanks in string
            string[] tokenized = splitString.Split(' '); //Split the string
            try
            {
                foreach (string token in tokenized)
                {
                    int value = Convert.ToInt32(token, 16);
                    id.Insert(0, (byte)value);
                }
            }
            catch(FormatException)
            {
                throw new FormatException("The input must be a valid hex number");
            }
            return id.ToArray();
        }

        public static byte[] GetIdentifierData(XmlNode identifierNode)
        {
            return GetIdentifierData(identifierNode, out _);
        }

        public static byte[] GetIdentifierData(XmlNode identifierNode, out bool isString)
        {
            if(identifierNode.Attributes["identifier"] != null)
            {
                string data = identifierNode.Attributes["identifier"].Value;
                try
                {
                    isString = false;
                    return GetHex(data);
                }
                catch (FormatException)
                {
                    isString = true;
                    return Encoding.ASCII.GetBytes(data);
                }
            }
            isString = false;
            return new byte[0];
        }

        public void AddSymbol(Symbol symbol)
        {
            symbols.Add(symbol);
        }

        public Func<IEnumerable<byte>,bool> GetMatchFunction()
        {
            return (data) =>
            {
                return Match(data);
            };
        }

        public virtual bool Match(IEnumerable<byte> data)
        {
            bool groupmatched = true;
            if(Group != null)
            {
                groupmatched = Group.DataMatch(data);
                data = data.Skip(Group.ID.Length);
            }

            groupmatched = groupmatched && data.Take(identifier.Length).SequenceEqual(identifier);
            return groupmatched;
        }

        public int GetBitlength()
        {
            int ret = identifier.Length * 8;
            foreach(Symbol symbol in symbols)
            {
                ret += symbol.GetPaddedBitlength();
            }
            return ret;
        }

        public int GetRawBitLength()
        {
            int ret = identifier.Length * 8;
            foreach (Symbol symbol in symbols)
            {
                ret += symbol.GetBitlength();
            }
            return ret;
        }

        public int GetMaximumBitlength()
        {
            int ret = identifier.Length * 8;
            foreach (Symbol symbol in symbols)
            {
                ret += symbol.GetMaxBitlength();
            }
            return ret;
        }

        public int GetMaximumByteLength()
        {
            return GetMaximumBitlength() / 8;
        }

        public int GetByteLength()
        {
            if(identifier != null)
            {
                return identifier.Length + GetByteLengthWithoutID();
            }
            return GetByteLengthWithoutID();
        }

        public int GetByteLengthWithoutID()
        {
            int ret = 0;
            foreach (Symbol symbol in symbols)
            {
                ret += symbol.ByteCount;
            }
            return ret;
        }

        public float BitstuffingFactor()
        {
            return GetBitlength() / GetRawBitLength();
        }

        public IEnumerator<Symbol> GetEnumerator()
        {
            return symbols.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsVariableLength
        {
            get
            {
                foreach (Symbol symbol in this)
                {
                    if (symbol.IsVariableLength)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public MessageGroup Group { get; set; } = null;

        public Symbol FindSymbol(string symbolname)
        {
            foreach(var symb in this)
            {
                if(symb.Name == symbolname)
                {
                    return symb;
                }
            }
            return null;
        }
    }
}
