using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace libxcm.Types
{
    public class Bool : IType, IEquatable<Bool>, IEquatable<bool>
    {
        public bool value;
        public int Bitlength { get; set; } = 1;

        public bool Signed => false;

        public int SetValue(byte[] data) => SetValue(data[0]);
        public int SetValue(byte data)
        {
            value = (data & 0x01) != 0;
            return Bitlength;
        }
        public void SetValue(string value)
        {
            this.value = value.ToLower() == "true";
        }

        public byte[] GetValue()
        {
            if(value)
            {
                return new byte[] { 1 };
            }
            else
            {
                return new byte[] { 0 };
            }
        }

        public object GetValueObject() => value;

        public override string ToString() => value.ToString();

        public void SetValue(object data)
        {
            value = (bool)data;
        }

        public IType Copy()
        {
            return new Bool()
            {
                Bitlength = this.Bitlength,
                value = this.value
            };
        }

        public bool Equals(Bool other) => other.value == value;

        public static bool operator ==(Bool lhs, Bool rhs) => lhs != null && lhs.Equals(rhs);
        public static bool operator !=(Bool lhs, Bool rhs) => !(lhs == rhs);

        public static implicit operator bool(Bool b) => b.value;

        public override bool Equals(object obj) => Equals(obj as Bool);

        public override int GetHashCode() => value.GetHashCode();

        public bool Equals(bool other) => value == other;
    }
}
