using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Linq;
using System.IO;

namespace libxcm
{
    public class Symbol : IEnumerable<Entry>
    {
        protected List<Entry> entries = new List<Entry>();
        private readonly int nativeNodeCount = 0;

        private static Entry EntryFactory(XmlNode entryNode)
        {
            return new Entry(entryNode);
        }

        public Symbol(XmlNode node, bool isNested = false) : this(node, EntryFactory, isNested)
        {

        }

        protected Symbol(XmlNode node, Func<XmlNode, Entry> entryFactory, bool isNested)
        {
            if (node.Attributes["name"] == null)
            {
                if(!isNested)
                {
                    throw new ArgumentException("A non nested symbol must define a name");
                }
                IsAnonymous = true;
                Name = "";
            }
            else
            {
                Name = node.Attributes["name"].Value;
            }
            using (MemoryStream str = new MemoryStream())
            {
                var sw = new StreamWriter(str);
                foreach (XmlNode entryXML in node)
                {
                    if (entryXML.NodeType == XmlNodeType.Element)
                    {
                        Entry entry = entryFactory(entryXML);
                        if (entries.Any(x => x.Name == entry.Name))
                        {
                            throw new ArgumentException("An entry must have a unique name in the symbol");
                        }
                        if (!entry.IsVirtual)
                        {
                            if (entry.MaxBitLength % 8 != 0)
                            {
                                IsBitfield = true;
                            }
                            if (entry.IsVariableLength)
                            {
                                IsVariableLength = true;
                            }
                        }
                        nativeNodeCount++;
                        entries.Add(entry);
                    }
                    else if (entryXML.NodeType == XmlNodeType.Comment)
                    {
                        sw.WriteLine(entryXML.Value);
                    }
                }
                if(IsBitfield && IsVariableLength)
                {
                    throw new ArgumentException("A symbol cannot have a variabel length while beeing a bitfield");
                }
                sw.Flush();
                str.Position = 0;
                DocuText.Add((new StreamReader(str)).ReadToEnd());
            }
        }

        public List<string> DocuText { get; set; } = new List<string>();

        public bool IsAnonymous { get; protected set; } = false;

        public string Name { get; set; }

        //Copy constructor
        protected Symbol(Symbol symbol, bool fullCopy = false)
        {
            Name = symbol.Name;
            int count;
            if (fullCopy)
            {
                count = symbol.entries.Count;
            }
            else
            {
                count = symbol.nativeNodeCount;
            }
            foreach(var entry in symbol.entries)
            {
                entries.Add(entry.Copy());
                count--;
                if (count <= 0)
                    break;
            }
        }

        public bool Modified { get; protected set; } = false;
        public virtual int GetPaddingLength()
        {
            int totalBitLength = GetBitlength();
            if (totalBitLength % 8 != 0)
            {
                return 8 - totalBitLength % 8;
            }
            return 0;
        }

        public virtual void Add(Entry entry)
        {
            if (entries.Any(x => x.Name == entry.Name))
            {
                throw new ArgumentException("An entry must have a unique name in the symbol");
            }
            Modified = true;
            if (!entry.IsVirtual)
            {
                if (entry.MaxBitLength % 8 != 0)
                {
                    IsBitfield = true;
                }
                if (entry.IsVariableLength)
                {
                    IsVariableLength = true;
                }
            }
            if (IsBitfield && IsVariableLength)
            {
                throw new ArgumentException("A symbol cannot have a variabel length while beeing a bitfield");
            }
            entries.Add(entry);
        }

        public virtual IEnumerator<Entry> GetEnumerator()
        {
            return entries.GetEnumerator();
        }
        public virtual int GetPaddedBitlength()
        {
            int totalBitlength = GetBitlength();
            return totalBitlength + GetPaddingLength();
        }
        public virtual int GetBitlength()
        {
            int ret = 0;
            foreach(Entry entry in entries)
            {
                if (entry.IsVirtual) //Skip virtual entries
                    continue;
                ret += entry.Bitlength;
            }
            return ret;
        }

        public virtual int GetMaxBitlength()
        {
            int ret = 0;
            foreach (Entry entry in entries)
            {
                if (entry.IsVirtual) //Skip virtual entries
                    continue;
                ret += entry.MaxBitLength;
            }
            return ret;
        }

        public int ByteCount { get => GetPaddedBitlength() / 8; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual Symbol Copy(bool fullCopy = false)
        {
            return new Symbol(this, fullCopy);
        }

        public bool IsBitfield { get; protected set; } = false;

        public bool IsVariableLength { get; protected set; } = false;

        public bool IsGlobal { get; set; } = false;

        public Entry FindEntry(string entryname)
        {
            foreach(var entry in this)
            {
                if(entry.Name == entryname)
                {
                    return entry;
                }
            }
            return null;
        }

        public override string ToString()
        {
            List<string> values = new List<string>();
            foreach(var entry in this)
            {
                values.Add(entry.Name + ": ");
                values.Add(entry.ToString().Trim());
            }
            return String.Join(" ",values);
        }
    }
}
