using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace libxcm
{
    public class XCMDokument
    {
        private readonly List<Symbol> symbols = new ();
        private readonly Dictionary<string, XCMTokenizer> tokenizers = new ();

        private readonly Dictionary<string, Connection> incommingConnections = new();
        private readonly Dictionary<string, Connection> outgoingConnections = new();
        public XCMDokument(XmlDocument doc, ITokenizerFactory factory) : this(doc.DocumentElement, factory)
        {

        }

        public XCMDokument(XmlElement root, ITokenizerFactory factory)
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
                var connection = new Connection(incon);
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
                var connection = new Connection(outcon, true);
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
                tokenizer.Name = systemname;
                tokenizers.Add(systemname, tokenizer);
            }
        }

        public async Task StartConnectionsAsync(CancellationToken token)
        {
            List<Task> tasks = new();
            foreach(var connection in incommingConnections.Values)
            {
                tasks.Add(connection.StartStreamAsync(token));
            }
            foreach (var connection in outgoingConnections.Values)
            {
                tasks.Add(connection.StartStreamAsync(token));
            }

            await Task.WhenAll(tasks);
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
