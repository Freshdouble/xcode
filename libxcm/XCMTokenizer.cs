using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;

namespace libxcm
{
    public class XCMTokenizer
    {
        protected Dictionary<string, List<object>> compiledtokens = new Dictionary<string, List<object>>();
        protected List<Symbol> knownSymbols = new List<Symbol>();
        public XCMTokenizer(XmlDocument doc) : this(doc.DocumentElement, new XmlNamespaceManager(doc.NameTable))
        {

        }
        public XCMTokenizer(XmlElement root, XmlNamespaceManager mngr)
        {
            mngr.AddNamespace("sps", "http://www.w3schools.com");
            foreach(XmlNode token in root)
            {
                foreach(XmlNode t in token)
                {
                    if(t.NodeType != XmlNodeType.Comment)
                    {
                        BuildToken(token, t);
                    }
                }
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

        virtual public Message BuildMessage(XmlNode node)
        {
            return new Message(node, knownSymbols);
        }

        virtual public Command BuildCommand(XmlNode node)
        {
            return new Command(node, knownSymbols);
        }

        virtual public Symbol BuildSymbol(XmlNode node)
        {
            return new Symbol(node);
        }

        virtual public MessageGroup BuildGroup(XmlNode node)
        {
            return new MessageGroup(node, knownSymbols, this);
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
                    compiledtokens["message"].Add(BuildMessage(node));
                    break;
                case "command":
                    if (!compiledtokens.ContainsKey("command"))
                    {
                        compiledtokens.Add("command", new List<object>());
                    }
                    compiledtokens["command"].Add(BuildCommand(node));
                    break;
                case "Group":
                    MessageGroup group = BuildGroup(node);
                    foreach(Message msg in group.Messages)
                    {
                        compiledtokens["message"].Add(msg);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
