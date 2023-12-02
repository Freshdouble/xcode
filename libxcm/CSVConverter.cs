using libxcm;
using System.Text;
using System.Text.Json;
using libxcm.JsonTypes;
using System.Collections.Generic;
using System.IO;

namespace xcmparser
{
    class CsvConverter : IMessageConverter
    {
        public void SendConvertedMessage(libconnection.StreamPipe pipe, Message msg)
        {
            foreach (Symbol symbol in msg)
            {
                using MemoryStream memorystream = new();
                string symbolbasename = msg.Name;
                if(!symbol.IsAnonymous)
                {
                    symbolbasename += "_" + symbol.Name;
                }
                StreamWriter sr = new(memorystream);
                sr.Write(symbolbasename + ";");
                foreach (Entry entry in symbol)
                {
                    sr.Write(entry.GetValue<object>().ToString() + ";");
                }
                sr.WriteLine();
                string csvline = Encoding.ASCII.GetString(memorystream.ToArray());
                pipe.TransmitMessage(new libconnection.Message(csvline));
            }
        }
    }
}