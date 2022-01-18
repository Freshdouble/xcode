using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace xcodegen
{
    [XmlRoot("type")]
    public class DataType
    {
        [XmlElement("internal")]
        public string internalname;
        [XmlElement("bitlength")]
        public int bitlength;
        [XmlElement("signed")]
        public bool signed;
        [XmlElement("typename")]
        public string datatypename;
        [XmlElement("varlength")]
        public bool varlength;
    }
}
