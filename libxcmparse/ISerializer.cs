using libxcm;
using System;
using System.Collections.Generic;
using System.Text;

namespace libxcmparse
{
    public interface ISerializer<T>
    {
        T Serialize(XCMTokenizer tokenizer);
        T Serialize(Message message);
        T Serialize(Symbol symbol);
        T Serialize(Entry entry);
        object GetDataObject(Message message);
        object GetDataObject(Symbol symbol);
        object GetDataObject(Entry entry);
    }
}
