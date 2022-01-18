using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace libxcm.Types
{
    public class FixedNumber : AbstractNumericalType, IEquatable<FixedNumber>, IEquatable<long>, IEquatable<int>
    {
        private readonly int bitlength;
        private readonly byte[] bitmask;
        public byte[] value;
        public byte[] lastValue = null;

        public FixedNumber(int bitlength, bool signed) : this(bitlength, signed, "0")
        {

        }
        public FixedNumber(int bitlength, bool signed, string value)
        {
            if (bitlength > 64)
                throw new ArgumentException("A maximum bitsize of 64 bits is allowed");
            if (bitlength <= 0)
                throw new ArgumentException("The bitsize must be greater than zero when defining a fixed number");
            this.bitlength = bitlength;
            Signed = signed;
            bitmask = GetBitMask(bitlength);
            long val = long.Parse(value);
            this.value = ApplyBitmask(BitConverter.GetBytes(val), bitmask);
        }

        public static byte[] ApplyBitmask(byte[] data, byte[] bitmask)
        {
            int bytecount = Math.Min(data.Length, bitmask.Length);
            byte[] ret = new byte[bytecount];
            for (int i = 0; i < bytecount; i++)
            {
                ret[i] = (byte)(data[i] & bitmask[i]);
            }
            return ret;
        }

        public static byte[] GetBitMask(int bitlength)
        {
            byte[] mask = new byte[(int)Math.Ceiling((double)bitlength / 8.0)];
            int i = 0;
            for (i = 0; i < bitlength / 8; i++) //Fill all bytes that are full with ones
            {
                mask[i] = 0xFF;
            }
            if (i < mask.Length)
            {
                mask[i] = 0;
                for (int d = 0; d < bitlength % 8; d++)
                {
                    mask[i] |= (byte)(1 << d);
                }
            }
            return mask;
        }

        public override void SetValue(string number)
        {
            SetValue(long.Parse(number));
        }

        public int SetValue(long number)
        {
            if (!Signed)
            {
                if (number < 0)
                {
                    throw new ArgumentException("This number is unsigned");
                }
            }
            return SetValue(BitConverter.GetBytes(number));
        }
        public override int SetValue(byte[] data)
        {
            UpdateTimestamp();
            lastValue = value;
            value = ApplyBitmask(data, bitmask); 
            return Bitlength;
        }

        public override int Bitlength
        {
            get => bitlength; 
            protected set
            {
                
            }
        }

        public override bool Signed { get; protected set; }

        public override byte[] GetValue() => value;

        protected BigInteger GetIntegerValue(byte[] value)
        {
            List<byte> data = new List<byte>(value);
            if (!Signed)
            {
                data.Add(0);
            }
            else
            {
                int lastIndex = bitmask.Length - 1;
                //Get last one bit in mask
                int i;
                for (i = 0; i < 8; i++)
                {
                    if ((bitmask[lastIndex] & (1 << i)) == 0)
                    {
                        i--;
                        break;
                    }
                }
                //Check if this bit is set in the data
                bool isnegative = (value[lastIndex] & (1 << i)) != 0;
                if (isnegative)
                {
                    bool carry = false;
                    //Subtrakt 1
                    for (i = data.Count - 1; i >= 0; i--)
                    {
                        int val = data[i] - 1;
                        if (carry)
                            val -= 1;
                        if (val < 0)
                            carry = true;
                        else
                            carry = false;
                        data[i] = (byte)val;
                    }
                    //Invert all bits
                    for (i = 0; i < data.Count; i++)
                    {
                        data[i] = (byte)~data[i];
                    }
                    //Apply bitmask
                    byte[] d = ApplyBitmask(data.ToArray(), bitmask);
                    BigInteger positive = new BigInteger(d);
                    return positive * (-1); //Return the actual value
                }
            }
            return new BigInteger(data.ToArray());
        }

        public BigInteger GetIntegerValue() => GetIntegerValue(value);

        public override object GetValueObject() => GetIntegerValue();

        public override string ToString() => GetIntegerValue().ToString();

        public override void SetValue(object data)
        {
            SetValue((long)data);
        }

        public override object GetChange()
        {
            if(value != null && lastValue != null)
            {
                return GetIntegerValue(value) - GetIntegerValue(lastValue);
            }
            return null;
        }

        public override object GetDerivate(int timediff)
        {
            if (timediff > 0)
            {
                BigInteger change = (BigInteger)GetChange();
                return (change / timediff) * 1000;
            }
            return 0;
        }

        public override IType Copy()
        {
            return new FixedNumber(Bitlength, Signed)
            {
                Bitlength = Bitlength,
                lastTimestamp = lastTimestamp,
                LastTimestamp = LastTimestamp,
                lastValue = lastValue,
                Signed = Signed,
                startTime = startTime,
                TimestampDiff = TimestampDiff,
                value = value
            };
        }

        public bool Equals(FixedNumber other) => GetIntegerValue() == other.GetIntegerValue();

        public static bool operator ==(FixedNumber lhs, FixedNumber rhs) => lhs != null && lhs.Equals(rhs);
        public static bool operator !=(FixedNumber lhs, FixedNumber rhs) => !(lhs == rhs);

        public static implicit operator int(FixedNumber f) => (int)f.GetIntegerValue();
        public static implicit operator long(FixedNumber f) => (long)f.GetIntegerValue();

        public override bool Equals(object obj) => Equals(obj as FixedNumber);

        public override int GetHashCode() => GetIntegerValue().GetHashCode();

        public bool Equals(int other) => GetIntegerValue() == other;

        public bool Equals(long other) => GetIntegerValue() == other;
    }
}
