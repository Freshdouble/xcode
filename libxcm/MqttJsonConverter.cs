using libxcm;
using System.Text;
using System.Text.Json;
using libxcm.JsonTypes;
using System.Collections.Generic;

namespace xcmparser
{
    class MqttJsonConverter : IMessageConverter
    {
        private static void AddTag(ref string topic, string tag)
        {
            topic += "_" + tag;
        }

        public void SendConvertedMessage(libconnection.StreamPipe pipe, Message msg)
        {
            foreach (Symbol symbol in msg)
            {
                foreach (Entry entry in symbol)
                {
                    string mqttTopic = msg.Name;
                    if(!mqttTopic.EndsWith("/"))
                    {
                        mqttTopic += "/";
                    }
                    if(!symbol.IsAnonymous)
                    {
                        AddTag(ref mqttTopic, symbol.Name);
                    }
                    AddTag(ref mqttTopic, entry.Name);
                    string json = JsonSerializer.Serialize(new
                    {
                        value = entry.GetValue<object>()
                    });
                    pipe.TransmitMessage(new libconnection.Message(json)
                    {
                        CustomObject = mqttTopic
                    });
                }
            }
        }
    }
}