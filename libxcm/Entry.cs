using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using libxcm.Types;
using String = libxcm.Types.String;

namespace libxcm
{
    public class Entry
    {
        public Entry(XmlNode entryNode)
        {
            if (entryNode.Attributes["isvisible"] != null)
            {
                IsVisible = entryNode.Attributes["isvisible"].Value.ToLower() == "true";
            }
            if (entryNode.Attributes["name"] == null)
            {
                Name = "AnonymousEntry";
                IsAnonymous = true;
            }
            else
            {
                Name = entryNode.Attributes["name"].Value;
                IsAnonymous = false;
            }

            if (entryNode.Attributes["visibility"] != null && entryNode.Attributes["visibility"].Value == "virtual")
            {
                IsVirtual = true;
            }

            if (entryNode.Attributes["type"] == null)
            {
                throw new ArgumentException("The type attribute must be set on the entry node");
            }
            string type = entryNode.Attributes["type"].Value;
            int bitlength = 0;
            if (entryNode.Attributes["bitlength"] != null)
            {
                bitlength = int.Parse(entryNode.Attributes["bitlength"].Value);
            }

            string valueString = null;
            if (entryNode.Attributes["value"] != null)
            {
                valueString = entryNode.Attributes["value"].Value;
            }

            if(entryNode.Attributes["fixed"] != null)
            {
                IsFixedCommand = entryNode.Attributes["fixed"].Value.ToLower() == "true";
            }

            switch (type.ToLower())
            {
                case "int":
                    BaseType = "fixed";
                    if (bitlength == 0)
                    {
                        throw new ArgumentException("The bitlength must be set when defining integers");
                    }
                    Value = new FixedNumber(bitlength, true);
                    if (valueString != null)
                    {
                        Value.SetValue(valueString);
                    }
                    break;
                case "uint":
                    BaseType = "fixed";
                    if (bitlength == 0)
                    {
                        throw new ArgumentException("The bitlength must be set when defining integers");
                    }
                    Value = new FixedNumber(bitlength, false);
                    if (valueString != null)
                    {
                        Value.SetValue(valueString);
                    }
                    break;
                case "bool":
                    BaseType = "bool";
                    if (bitlength == 0)
                    {
                        Value = new Bool()
                        {
                            value = false
                        };
                    }
                    else
                    {
                        Value = new Bool()
                        {
                            value = false,
                            Bitlength = bitlength
                        };
                    }
                    if (valueString != null)
                    {
                        Value.SetValue(valueString);
                    }
                    break;
                case "double":
                    BaseType = "double";
                    Value = new FloatingPointNumber(FloatingPointType.@double);
                    if (valueString != null)
                    {
                        Value.SetValue(valueString);
                    }
                    break;
                case "float":
                    BaseType = "float";
                    Value = new FloatingPointNumber(FloatingPointType.@float);
                    if (valueString != null)
                    {
                        Value.SetValue(valueString);
                    }
                    break;
                case "string":
                    BaseType = "string";
                    if (entryNode.Attributes["length"] != null)
                    {
                        int strlength = int.Parse(entryNode.Attributes["length"].Value);
                        Value = new String(strlength, true); //This is a fixed length string
                    }
                    else
                    {
                        if (entryNode.Attributes["maxlength"] != null)
                        {
                            int maxlength = int.Parse(entryNode.Attributes["maxlength"].Value);
                            Value = new String(maxlength); //This is a non fixed length string with a maximum length
                        }
                        else
                        {
                            throw new ArgumentException("A string with variable length must have a maximum length! " + Name);
                        }
                    }
                    if(valueString != null)
                    {
                        ((String)Value).SetValue(valueString);
                    }
                    break;
                default: throw new ArgumentException(string.Format("The type {0} is not valid", type));

            }

            using (MemoryStream str = new MemoryStream())
            {
                var sw = new StreamWriter(str);
                foreach (XmlNode child in entryNode)
                {
                    if (child.NodeType == XmlNodeType.Comment)
                    {
                        sw.WriteLine(child.Value);
                    }
                }
                sw.Flush();
                str.Position = 0;
                DocuText.Add((new StreamReader(str)).ReadToEnd());
            }
        }

        public bool IsFixedCommand {get; protected set;} = false;

        public List<string> DocuText { get; set; } = new List<string>();

        public int Bitlength => Value.Bitlength;
        public string Name { get; set; }

        public string BaseType { get; private set; }

        public bool Signed { get => Value.Signed; }

        public bool IsVisible { get; protected set; } = true;

        public bool IsAnonymous { get; protected set; } = false;

        public IType Value { get; set; }

        public T GetValue<T>()
        {
            return (T)Value.GetValueObject();
        }

        public string GetValue()
        {
            return Value.ToString();
        }

        public bool TryGetValue<T>(ref T value)
        {
            try
            {
                value = GetValue<T>();
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public int MaxBitLength
        {
            get
            {
                if(Value is IVariableLength varlength)
                {
                    return varlength.MaxBitLength;
                }
                else
                {
                    return Value.Bitlength;
                }
            }
        }

        public bool IsVariableLength
        {
            get
            {
                if(Value is IVariableLength varlength)
                {
                    return !varlength.IsFixedLength;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsVirtual { get; protected set; } = false;

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
