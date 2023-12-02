using libconnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;

namespace libxcm
{
    public class Connection : StreamPipe
    {
        private readonly object _lock = new ();
        public Connection(XmlNode node, bool reverse = false) : base()
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
                    switch(stage.Name.ToLower())
                    {
                        case "stage":
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
                        break;

                        case "converter":
                            MessageConverter = GetConverterFromName(stage.InnerText);
                        break;
                    }
                }
            }
        }

        public static IMessageConverter GetConverterFromName(string name)
        {
            switch(name.ToLower())
            {
                case "json":
                    return new xcmparser.JsonConverter();
                case "mqttjson":
                    return new xcmparser.MqttJsonConverter();
                case "csv":
                    return new xcmparser.CsvConverter();
            }
            throw new ArgumentException($"No message converter with name {name} is available");
        }

        public IMessageConverter MessageConverter { get; set; } = new xcmparser.JsonConverter();

        public void TransmitParsedData(Message msg)
        {
            MessageConverter.SendConvertedMessage(this, msg);
        }

        public void TransmitParsedDataSynchronized(Message msg)
        {
            lock(_lock)
            {
                TransmitParsedData(msg);
            }
        }
    }
}
