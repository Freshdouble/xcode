using libxcmparse.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xcmparser;

namespace libxcmparse
{
    public class RecieveDataHandler
    {
        private List<DataMessage> dataMessages = new List<DataMessage>();

        public RecieveDataHandler(XCMParserTokenizer tokenizer)
        {
            dataMessages.AddRange(tokenizer.GetObjects<DataMessage>());
            Tokenizer = tokenizer;
        }

        public XCMParserTokenizer Tokenizer { get; }

        public bool ReceiveData(IEnumerable<byte> data)
        {
            bool hasmatched = false;
            foreach(var msg in dataMessages)
            {
                if(msg.Match(data))
                {
                    msg.ParseMessage(data.Skip(msg.IDByteLength + msg.IDOffset));
                    hasmatched = true;
                }
            }
            return hasmatched;
        }
    }
}
