using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using libxcm;
using libxcmparse.DataObjects;

namespace xcmparser
{
    public class XCMParserTokenizer : XCMTokenizer
    {
        public XCMParserTokenizer(XmlNode root, IEnumerable<Symbol> symbols, Dictionary<string, Connection> inboundConnection, Dictionary<string, Connection> outboundConnection) : base(root, symbols, inboundConnection, outboundConnection)
        {
            foreach(var s in symbols)
            {
                if(s is not DataSymbol)
                {
                    throw new ArgumentException("The parser tokenizer only supports datasymbols");
                }
            }
        }

        public override Symbol BuildSymbol(XmlNode node)
        {
            return new DataSymbol(node);
        }

        public override Command BuildCommand(XmlNode node, Connection inbound, Connection outbound, string systemname)
        {
            return new DataCommand(node, knownSymbols, inbound, outbound, systemname);
        }

        public override Message BuildMessage(XmlNode node, Connection inbound, Connection outbound, string systemname)
        {
            return new DataMessage(node, knownSymbols, inbound, outbound, systemname);
        }
    }
}
