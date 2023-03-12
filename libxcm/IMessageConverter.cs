using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libxcm
{
    public interface IMessageConverter
    {
        public byte[] ConvertToByteArray(Message msg);
    }
}
