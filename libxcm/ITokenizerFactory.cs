using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace libxcm
{
    public interface ITokenizerFactory
    {
        XCMTokenizer BuildTokenizer(XmlNode root, Dictionary<string, Connection> inboundConnection, Dictionary<string, Connection> outboundConnection);
        XCMTokenizer BuildTokenizer(XmlNode root, IEnumerable<Symbol> symbols, Dictionary<string, Connection> inboundConnection, Dictionary<string, Connection> outboundConnection);
    }
}
