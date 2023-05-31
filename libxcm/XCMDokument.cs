using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;

namespace libxcm
{
    public class XCMDokument
    {
        private List<Symbol> symbols = new List<Symbol>();
        private Dictionary<string, XCMTokenizer> tokenizers = new Dictionary<string, XCMTokenizer>();

        private Dictionary<string, Connection> incommingConnections = new Dictionary<string, Connection>();
        private Dictionary<string, Connection> outgoingConnections = new Dictionary<string, Connection>();
        public XCMDokument(XmlDocument doc, ITokenizerFactory factory, CancellationTokenSource cts) : this(doc.DocumentElement, factory, cts)
        {

        }

        public XCMDokument(XmlElement root, ITokenizerFactory factory, CancellationTokenSource cts)
        {
            foreach (XmlElement incon in root.SelectNodes("/spacesystem/connections/incomming/connection")) //Read all incomming connections and generate connection elements from it
            {
                string connName = incon.GetAttribute("name");
                if(string.IsNullOrWhiteSpace(connName))
                {
                    throw new ArgumentException("Each connection element must have a name");
                }
                if(incommingConnections.ContainsKey(connName))
                {
                    throw new ArgumentException("Each connection element must have a unique name");
                }
                var connection = new Connection(incon, cts.Token);
                incommingConnections.Add(connName, connection);
            }

            foreach (XmlElement outcon in root.SelectNodes("/spacesystem/connections/outgoing/connection")) //Read all outgoing connections and generate connection elements from it
            {
                string connName = outcon.GetAttribute("name");
                if (string.IsNullOrWhiteSpace(connName))
                {
                    throw new ArgumentException("Each connection element must have a name");
                }
                if (outgoingConnections.ContainsKey(connName))
                {
                    throw new ArgumentException("Each connection element must have a unique name");
                }
                var connection = new Connection(outcon, cts.Token, true);
                outgoingConnections.Add(connName, connection);
            }

            foreach(XmlElement globalSymbol in root.SelectNodes("/spacesystem/symbols/symbol"))
            {

            }

            foreach (XmlElement systemNode in root.SelectNodes("/spacesystem/system"))
            {
                string systemname = systemNode.Attributes["name"].Value;
                if (systemname == null || tokenizers.ContainsKey(systemname))
                    throw new ArgumentException("A system must have a unique name");
                var tokenizer = factory.BuildTokenizer(systemNode, symbols, incommingConnections, outgoingConnections);
                tokenizers.Add(systemname, tokenizer);
            }
        }

        public IEnumerable<XCMTokenizer> GetTokenizers()
        {
            return tokenizers.Values;
        }

        public XCMTokenizer GetTokenzierByName(string name)
        {
            return tokenizers[name];
        }
    }
}
