using libxcm;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace xcodegen
{
    class TypeGenerator
    {
        private List<DataType> dtypes = new List<DataType>();
        private static XmlDocument GetDocFromFile(string filename)
        {
            XmlDocument ret = new XmlDocument();
            ret.Load(filename);
            return ret;
        }
        public TypeGenerator(string xmlfile) : this(GetDocFromFile(xmlfile))
        {

        }

        public TypeGenerator(XmlDocument doc) : this(doc.DocumentElement)
        {

        }

        public string DefaultSinged { get; private set; } = "";
        public string DefaultUnsinged { get; private set; } = "";

        public string GetDefault(bool signed)
        {
            if(signed)
            {
                return DefaultSinged;
            }
            else
            {
                return DefaultUnsinged;
            }
        }

        public TypeGenerator(XmlElement rootelement)
        {
            foreach(XmlElement child in rootelement)
            {
                if(child.NodeType == XmlNodeType.Element)
                {
                    dtypes.Add(Deserialize<DataType>(child));
                }
            }
        }

        public DataType FindType(Entry entry)
        {
            DataType ret = null;
            foreach(var type in dtypes)
            {
                if(entry.BaseType == type.internalname && entry.IsVariableLength == type.varlength && entry.Signed == type.signed)
                {
                    if(type.bitlength == 0)
                    {
                        //Is generic
                        ret = type;
                    }
                    else
                    {
                        if (type.bitlength == entry.Bitlength)
                        {
                            //is non generic
                            return type;
                        }
                    }
                }
            }
            return ret;
        }

        private static T Deserialize<T>(XmlNode data) where T : class, new()
        {
            if (data == null)
                return null;

            var ser = new XmlSerializer(typeof(T));
            using (var xmlNodeReader = new XmlNodeReader(data))
            {
                return (T)ser.Deserialize(xmlNodeReader);
            }
        }
    }
}
