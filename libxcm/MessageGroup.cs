using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;

namespace libxcm
{
    public class MessageGroup
    {
        /*
        protected readonly byte[] identifier;
        public MessageGroup(XmlNode groupNode, List<Symbol> knownSymbols, XCMTokenizer tokenizer)
        {
            identifier = Message.GetIdentifierData(groupNode);
            Messages = new List<Message>();

            if (groupNode.Attributes["name"] != null)
            {
                Name = groupNode.Attributes["name"].Value;
            }

            foreach (XmlNode messageNode in groupNode)
            {
                if(messageNode.NodeType != XmlNodeType.Comment)
                {
                    Message m = tokenizer.BuildMessage(messageNode);
                    m.Group = this;
                    Messages.Add(m);
                }
            }
        }

        public string Name { get; set; } = "";
        public MessageGroup(XmlNode groupNode, List<Symbol> knownSymbols, XCMTokenizer tokenizer, IEnumerable<Message> messages) : this(groupNode, knownSymbols, tokenizer)
        {
            Messages.AddRange(messages);
        }

        public List<Message> Messages { get; }

        public byte[] ID => identifier;

        public bool DataMatch(IEnumerable<byte> data)
        {
            bool matched = true;

            if(Group != null)
            {
                matched = Group.DataMatch(data);
                data = data.Skip(Group.ID.Length);
            }

            return matched && data.SequenceEqual(identifier);
        }

        public MessageGroup Group = null;
        */
    }
}
