using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libconnection;

namespace libxcm
{
    public interface IMessageConverter
    {
        public void SendConvertedMessage(StreamPipe pipe, Message msg);
    }
}
