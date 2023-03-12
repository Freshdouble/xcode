using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace libxcm
{
    public class XCMFactory : ITokenizerFactory
    {
        public virtual XCMTokenizer BuildTokenizer(XmlNode root, Dictionary<string, Connection> inboundConnection, Dictionary<string, Connection> outboundConnection)
        {
            return BuildTokenizer(root, null, inboundConnection, outboundConnection);
        }

        public XCMTokenizer BuildTokenizer(XmlNode root, IEnumerable<Symbol> symbols, Dictionary<string, Connection> inboundConnection, Dictionary<string, Connection> outboundConnection)
        {
            return new XCMTokenizer(root, symbols, inboundConnection, outboundConnection);
        }
    }
}
