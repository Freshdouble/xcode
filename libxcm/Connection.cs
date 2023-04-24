using libconnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace libxcm
{
    public class Connection : StreamPipe
    {
        private object _lock = new object();
        public Connection(XmlNode node, bool reverse = false)
        {
            var iterator = node.GetChildsOrdered();
            if(reverse)
            {
                iterator = iterator.Reverse();
            }
            foreach (XmlNode stage in iterator)
            {
                if(stage.NodeType != XmlNodeType.Comment)
                {
                    if(stage.Name.ToLower() == "stage")
                    {
                        string className = "";
                        Dictionary<string, string> parameters = new ();
                        foreach (XmlNode parameter in stage)
                        {
                            if (stage.Name.ToLower() == "stage")
                            {
                                switch(parameter.Name.ToLower())
                                {
                                    case "class":
                                        className = parameter.InnerText;
                                        break;
                                    case "parameter":
                                        string parameterName = parameter.Attributes["name"]?.Value;
                                        if(string.IsNullOrWhiteSpace( parameterName ) )
                                        {
                                            throw new ArgumentException("The connection class parameter must have a name attribute.");
                                        }
                                        parameters.Add(parameterName, parameter.InnerText);
                                        break;
                                }
                            }
                        }
                        if(className == "")
                        {
                            throw new ArgumentException("A connection must have a class element inside it");
                        }
                        var datastream = NameResolver.GetStreamByName(className, parameters);
                        if (datastream == null)
                            throw new ArgumentException($"There is no connection class with the name {className}");

                        Add(datastream);
                    }
                }
            }
        }

        public IMessageConverter MessageConverter { get; set; } = new xcmparser.JsonConverter();

        public void TransmitData(Message msg)
        {
            var data = MessageConverter.ConvertToByteArray(msg);
            TransmitMessage(new libconnection.Message(data));
        }

        public void TransmitDataSynchronized(Message msg)
        {
            lock(_lock)
            {
                TransmitData(msg);
            }
        }
    }
}
