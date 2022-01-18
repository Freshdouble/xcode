using System;
using System.Collections.Generic;
using System.Text;

namespace libxcm.Types
{
    public abstract class AbstractNumericalType : INumericalType
    {
        protected DateTime startTime = DateTime.UtcNow;
        protected DateTime lastTimestamp = DateTime.UtcNow;

        protected int TimestampNow => (int)(DateTime.UtcNow.Subtract(startTime)).TotalMilliseconds;
        public int TimestampDiff { get; protected set; } = 0;

        public int LastTimestamp { get; protected set; } = 0;

        abstract public bool Signed { get; protected set; }

        abstract public int Bitlength { get; protected set; }

        abstract public object GetChange();

        public object GetDerivate()
        {
            return GetDerivate(TimestampDiff);
        }

        abstract public object GetDerivate(int timediff);

        abstract public byte[] GetValue();

        abstract public object GetValueObject();

        abstract public int SetValue(byte[] data);

        abstract public void SetValue(object data);

        protected void UpdateTimestamp()
        {
            DateTime now = DateTime.UtcNow;
            TimestampDiff = (int)now.Subtract(lastTimestamp).TotalMilliseconds;
            lastTimestamp = now;
            LastTimestamp = (int)now.Subtract(startTime).TotalMilliseconds;
        }

        public abstract IType Copy();
        public abstract void SetValue(string data);
    }
}
