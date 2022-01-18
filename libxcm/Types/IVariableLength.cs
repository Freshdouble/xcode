using System;
using System.Collections.Generic;
using System.Text;

namespace libxcm.Types
{
    public interface IVariableLength
    {
        bool IsFixedLength { get; }
        int MaxBitLength { get; set; }
    }
}
