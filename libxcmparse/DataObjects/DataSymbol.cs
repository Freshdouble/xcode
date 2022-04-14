using libxcm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace libxcmparse.DataObjects
{
    public class DataSymbol : Symbol
    {
        public DataSymbol(XmlNode node, bool isNested = false) : base(node, DataMessage.EntryFactory, isNested)
        {
            foreach(var entry in this)
            {
                if(entry is DataEntry dataentry)
                {
                    dataentry.Parent = this;
                }
            }
        }

        protected DataSymbol(Symbol symbol, bool fullCopy = false) : base(symbol, fullCopy)
        {
        }

        public int ParseMessage(IEnumerable<byte> message)
        {
            int bitoffset = 0;
            BitArray bitArr = new BitArray(message as byte[] ?? message.ToArray());
            foreach(DataEntry entry in this)
            {
                if (entry.IsVirtual)
                    continue;
                byte[] data = new byte[(int)Math.Ceiling(bitArr.Length / 8.0)];
                bitArr.CopyTo(data, 0);
                int bitlength = entry.SetValue(data);
                bitoffset += bitlength;
                bitArr.RightShift(bitlength);
            }

            return (int)Math.Ceiling(bitoffset / 8.0);
        }

        public byte[] GetData()
        {
            int bitoffset = 0;
            BitArray bitArray = new BitArray(ByteCount * 8);
            foreach(DataEntry entry in this)
            {
                BitArray entryBits = new BitArray(entry.Value.GetValue());
                for(int i = 0; i < entry.Bitlength; i++)
                {
                    bitArray[bitoffset + i] = entryBits[i];
                }
                bitoffset += entry.Bitlength;
            }
            byte[] ret = new byte[ByteCount];
            bitArray.CopyTo(ret, 0);
            return ret;
        }

        public bool CheckValidity()
        {
            bool ret = true;
            foreach(DataEntry entry in this)
            {
                ret &= entry.IsValid;
            }
            return ret;
        }

        public bool HasChecks
        {
            get
            {
                bool ret = false;
                foreach (DataEntry entry in this)
                {
                    ret |= entry.HasChecks;
                }
                return ret;
            }
        }

        public List<DataSymbol> Sibblings { get; set; } = new List<DataSymbol>();
        public object Parent { get; set; } = null;

        public override Symbol Copy(bool fullCopy = false)
        {
            return new DataSymbol(this, fullCopy);
        }
    }
}
