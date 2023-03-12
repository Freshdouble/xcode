using libxcm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using xcmparser;

namespace libxcmparse
{
    public class XCMParserTokenizerFactory : ITokenizerFactory
    {
        public XCMTokenizer BuildTokenizer(XmlNode root, Dictionary<string, Connection> inboundConnection, Dictionary<string, Connection> outboundConnection)
        {
            return BuildTokenizer(root, null, inboundConnection, outboundConnection);
        }

        public XCMTokenizer BuildTokenizer(XmlNode root, IEnumerable<Symbol> symbols, Dictionary<string, Connection> inboundConnection, Dictionary<string, Connection> outboundConnection)
        {
            return new XCMParserTokenizer(root, symbols, inboundConnection, outboundConnection);
        }
    }
}
