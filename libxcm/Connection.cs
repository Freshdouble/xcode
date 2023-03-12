using libconnection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;

namespace libxcm
{
    public class Connection : StreamPipe
    {
        public Connection(XmlNode node)
        {
            foreach(XmlNode stage in node)
            {
                if(stage.NodeType != XmlNodeType.Comment)
                {
                    if(stage.Name.ToLower() == "stage")
                    {
                        string className = "";
                        List<string> parameters = new List<string>();
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
                                        parameters.Add(parameter.InnerText);
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

        public void TransmitParsedData(Message msg)
        {
            var data = MessageConverter.ConvertToByteArray(msg);
            PublishDownstreamData(new libconnection.Message(data));
        }
    }
}
