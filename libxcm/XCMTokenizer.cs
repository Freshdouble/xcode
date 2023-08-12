using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Xml.Linq;

namespace libxcm
{
    public class XCMTokenizer
    {
        protected Dictionary<string, List<object>> compiledtokens = new Dictionary<string, List<object>>();
        protected List<Symbol> knownSymbols = new List<Symbol>();
        protected Connection inbound;
        protected Connection outbound;

        public XCMTokenizer(XmlNode root, IEnumerable<Symbol> symbols, Connection inboundConnection, Connection outboundConnection)
        {
            if (inboundConnection == null || outboundConnection == null)
            {
                throw new ArgumentException("Each system must have a default input and output connection");
            }

            inbound = inboundConnection;
            outbound = outboundConnection;

            if (symbols != null)
            {
                AddKownSymbols(symbols);
            }
            foreach (XmlNode token in root)
            {
                if (token.NodeType != XmlNodeType.Comment)
                {
                    foreach (XmlNode t in token)
                    {
                        if (t.NodeType != XmlNodeType.Comment)
                        {
                            BuildToken(token, t);
                        }
                    }
                }
            }
        }
        public XCMTokenizer(XmlNode root, IEnumerable<Symbol> symbols, Dictionary<string, Connection> inboundConnections, Dictionary<string, Connection> outboundConnections) : this(root, symbols, GetInboundConnectionFromXML(root, inboundConnections), GetOutboundConnectionFromXML(root, outboundConnections))
        {
        }

        public string Name { get; set; } = string.Empty;

        private static Connection GetInboundConnectionFromXML(XmlNode node, Dictionary<string, Connection> connections)
        {
            var defInput = node.SelectSingleNode("defaultInput");
            if(defInput == null) return null;
            var connName = defInput.InnerText;
            if(connections.ContainsKey(connName)) return connections[connName];
            else
            {
                return null;
            }
        }

        private static Connection GetOutboundConnectionFromXML(XmlNode node, Dictionary<string, Connection> connections)
        {
            var defInput = node.SelectSingleNode("defaultOutput");
            if (defInput == null) return null;
            var connName = defInput.InnerText;
            if (connections.ContainsKey(connName)) return connections[connName];
            else
            {
                return null;
            }
        }

        public IEnumerable<T> GetObjects<T>(bool strict = true)
        {
            foreach(var t in compiledtokens)
            {
                foreach(object obj in t.Value)
                {
                    if (strict)
                    {
                        if (obj.GetType() == typeof(T))
                        {
                            yield return (T)obj;
                        }
                    }
                    else
                    {
                        if (obj is T val)
                        {
                            yield return val;
                        }
                    }
                }
            }
        }

        virtual protected void AddKownSymbols(IEnumerable<Symbol> symbols)
        {
            foreach(var symb in symbols)
            {
                knownSymbols.Add(symb.Copy());
            }
        }

        virtual public Message BuildMessage(XmlNode node, Connection inbound, Connection outbound)
        {
            return new Message(node, knownSymbols, inbound, outbound);
        }

        virtual public Command BuildCommand(XmlNode node, Connection inbound, Connection outbound)
        {
            return new Command(node, knownSymbols, inbound, outbound);
        }

        virtual public Symbol BuildSymbol(XmlNode node)
        {
            return new Symbol(node);
        }

        virtual public MessageGroup BuildGroup(XmlNode node)
        {
            throw new NotImplementedException();
            //return new MessageGroup(node, knownSymbols, this);
        }
        virtual protected void BuildToken(XmlNode parent, XmlNode node)
        {
            switch(node.Name.ToLower())
            {
                case "symbol":
                    if(!compiledtokens.ContainsKey("symbol"))
                    {
                        compiledtokens.Add("symbol", new List<object>());
                    }
                    Symbol symb = BuildSymbol(node);
                    compiledtokens["symbol"].Add(symb);
                    knownSymbols.Add(symb);
                    break;
                case "message":
                    if (!compiledtokens.ContainsKey("message"))
                    {
                        compiledtokens.Add("message", new List<object>());
                    }
                    compiledtokens["message"].Add(BuildMessage(node, inbound, outbound));
                    break;
                case "command":
                    if (!compiledtokens.ContainsKey("command"))
                    {
                        compiledtokens.Add("command", new List<object>());
                    }
                    compiledtokens["command"].Add(BuildCommand(node, inbound, outbound));
                    break;
                    /*
                case "Group":
                    MessageGroup group = BuildGroup(node);
                    foreach(Message msg in group.Messages)
                    {
                        compiledtokens["message"].Add(msg);
                    }
                    break;*/
                default:
                    break;
            }
        }
    }
}
