using System;
using System.Collections.Generic;
using System.Text;

namespace libxcm.Types
{
    public interface IType
    {
        int SetValue(byte[] data);
        void SetValue(object data);

        void SetValue(string data);
        byte[] GetValue();
        bool Signed { get; }

        int Bitlength { get; }

        object GetValueObject();

        IType Copy();
    }
}
