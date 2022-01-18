using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace libxcm.Types
{
    public class String : IType, IVariableLength
    {
        protected string data = string.Empty;
        private Encoding encoding = Encoding.ASCII; //Default Encoding

        public String(int length, bool isfixed = false)
        {
            if(isfixed)
            {
                Stringlength = length;
                IsFixedLength = true;
                data = FillStringWith(string.Empty, length, '\0');
            }
            MaxBitLength = length * 8;
        }

        public String(int length, Encoding encoding) : this(length) => this.encoding = encoding;

        public bool Signed => false;

        public int Bitlength => encoding.GetByteCount(data) * 8;

        public int Stringlength { get; protected set; } = 0;

        public int MaxBitLength { get; set; }

        public bool IsFixedLength { get; protected set; } = false;

        public byte[] GetValue() => encoding.GetBytes(data);

        public int SetValue(byte[] data)
        {
            this.data = encoding.GetString(data.TakeWhile(x => x != 0).ToArray());
            int ret = this.data.Length * 8;
            if (!IsFixedLength && (data.Length > encoding.GetByteCount(this.data)))
            {
                ret += 8; //The string is zero terminated so add 8 bits to handle the string terminator
            }
            
            if(Stringlength > 0)
            {
                if(this.data.Length > Stringlength)
                {
                    this.data = this.data.Substring(0, Stringlength);
                    ret = Stringlength * 8;
                }
                else if(this.data.Length < Stringlength)
                {
                    int toadd = Stringlength - this.data.Length;
                    this.data = FillStringWith(this.data, toadd, '\0');
                }
            }
            return ret;
        }

        protected static string FillStringWith(string source, int length, char add)
        {
            while(length > 0)
            {
                source += add;
                length--;
            }
            return source;
        }

        public override string ToString() => data;

        public object GetValueObject() => data;

        public void SetValue(object data) => SetValue((string)data);

        public IType Copy()
        {
            return new String(Stringlength, IsFixedLength)
            {
                data = data,
                encoding = encoding,
                IsFixedLength = IsFixedLength,
                MaxBitLength = MaxBitLength,
                Stringlength = Stringlength
            };
        }

        public void SetValue(string data)
        {
            this.data = data;
            int maxlen = MaxBitLength / 8;
            if (maxlen > 0 && this.data.Length > maxlen)
            {
                this.data = this.data.Substring(0, maxlen);
            }
        }
    }
}
