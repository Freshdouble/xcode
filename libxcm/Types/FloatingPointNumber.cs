using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace libxcm.Types
{
    public enum FloatingPointType
    {
        @float,
        @double
    }
    public class FloatingPointNumber : AbstractNumericalType, IEquatable<FloatingPointNumber>, IEquatable<float>, IEquatable<double>
    {
        public double value = 0;
        public double lastValue = 0;
        public FloatingPointNumber(FloatingPointType type)
        {
            FloatingPointType = type;
            switch (type)
            {
                case FloatingPointType.@float:
                    Bitlength = sizeof(float) * 8;
                    break;
                case FloatingPointType.@double:
                    Bitlength = sizeof(double) * 8;
                    break;
                default:
                    throw new ArgumentException("Unkown floatingpoint type: " + type.ToString());
            }
            value = 0;
        }

        public FloatingPointType FloatingPointType { get; private set; }
        public override int Bitlength { get; protected set; }

        public override bool Signed { get; protected set; } = true;

        public override int SetValue(byte[] data)
        {
            UpdateTimestamp();
            lastValue = value;
            switch (FloatingPointType)
            {
                case FloatingPointType.@float:
                    value = BitConverter.ToSingle(data,0);
                    return sizeof(float) * 8;
                case FloatingPointType.@double:
                    value = BitConverter.ToDouble(data, 0);
                    return sizeof(double) * 8;
                default:
                    throw new ArgumentException("Unkown Floatingpointtype: " + FloatingPointType.ToString());
            }
        }

        public override void SetValue(string value)
        {
            UpdateTimestamp();
            lastValue = this.value;
            switch (FloatingPointType)
            {
                case FloatingPointType.@float:
                    this.value = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case FloatingPointType.@double:
                    this.value = double.Parse(value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        public override byte[] GetValue()
        {
            switch (FloatingPointType)
            {
                case FloatingPointType.@float:
                    return BitConverter.GetBytes((float)value);
                case FloatingPointType.@double:
                    return BitConverter.GetBytes(value);
            }
            throw new ArgumentException("Unkown floatingpoint type");
        }

        public override object GetValueObject() => value;

        public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

        public override void SetValue(object data)
        {
            switch (FloatingPointType)
            {
                case FloatingPointType.@float:
                    value = (float)data;
                    break;
                case FloatingPointType.@double:
                    value = (double)data;
                    break;
                default:
                    throw new ArgumentException("Unkown Floatingpointtype: " + FloatingPointType.ToString());
            }
        }

        public override object GetChange() => (value - lastValue);

        public override object GetDerivate(int timediff)
        {
            if(timediff > 0)
            {
                return ((double)GetChange() / timediff) * 1000.0;
            }
            return 0.0;
        }

        public override IType Copy()
        {
            return new FloatingPointNumber(FloatingPointType)
            {
                Bitlength = Bitlength,
                FloatingPointType = FloatingPointType,
                lastTimestamp = lastTimestamp,
                LastTimestamp = LastTimestamp,
                lastValue = lastValue,
                value = value,
                TimestampDiff = TimestampDiff,
                Signed = Signed,
                startTime = startTime
            };
        }

        public bool Equals(FloatingPointNumber other)
        {
            return value == other.value;
        }

        public static bool operator ==(FloatingPointNumber lhs, FloatingPointNumber rhs) => lhs != null && lhs.Equals(rhs);
        public static bool operator !=(FloatingPointNumber lhs, FloatingPointNumber rhs) => !(lhs == rhs);

        public override bool Equals(object obj) => Equals(obj as FloatingPointNumber);

        public override int GetHashCode() => value.GetHashCode();

        public bool Equals(double other) => value == other;

        public bool Equals(float other) => value == other;

        public static implicit operator float(FloatingPointNumber f) => (float)f.value;
        public static implicit operator double(FloatingPointNumber f) => (double)f.value;
    }
}
