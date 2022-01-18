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
        public XCMParserTokenizer(XmlDocument doc) : base(doc)
        {
        }

        public XCMParserTokenizer(XmlElement root, XmlNamespaceManager mngr) : base(root, mngr)
        {
        }

        public override Symbol BuildSymbol(XmlNode node)
        {
            return new DataSymbol(node);
        }

        public override Command BuildCommand(XmlNode node)
        {
            return new DataCommand(node, knownSymbols);
        }

        public override Message BuildMessage(XmlNode node)
        {
            return new DataMessage(node, knownSymbols);
        }
    }
}
