using System;
using System.Collections.Generic;
using System.Text;

namespace libxcm.Types
{
    public interface INumericalType : IType
    {
        int TimestampDiff { get; }
        int LastTimestamp { get; }
        object GetChange();
        object GetDerivate();
        object GetDerivate(int timediff);
    }
}
