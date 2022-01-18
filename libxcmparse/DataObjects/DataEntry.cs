using libxcm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using NCalc;
using libxcm.Types;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace libxcmparse.DataObjects
{
    public class DataEntry : Entry
    {
        public enum HistoryType
        {
            reset,
            roll
        }
        private Func<IType, object> converterFunction = null;
        private int historylength = 0;
        private HistoryType historytype = HistoryType.roll;
        private IType[] type = null;
        private int historyindex = 0;
        public DataEntry(XmlNode entryNode) : base(entryNode)
        {
            if(entryNode.Attributes["hashistory"] != null)
            {
                HasHistory = entryNode.Attributes["hashistory"].Value.ToLower() != "false";
            }
            if(HasHistory)
            {
                if (entryNode.Attributes["historylength"] == null) throw new ArgumentException("The history length must be specified");
                if (entryNode.Attributes["historytype"] == null) throw new ArgumentException("The history type must be specified");

                historylength = int.Parse(entryNode.Attributes["historylength"].Value);
                historytype = (HistoryType)Enum.Parse(typeof(HistoryType), entryNode.Attributes["historytype"].Value.ToLower());
            }
            if (entryNode.HasChildNodes)
            {
                foreach(XmlNode childNode in entryNode.ChildNodes)
                {
                    switch(childNode.Name.ToLower())
                    {
                        case "math":
                            Expression e = new Expression(childNode.FirstChild.Value);
                            converterFunction = (value) =>
                            {
                                e.EvaluateFunction += (string name, FunctionArgs args) =>
                                {
                                    switch (name)
                                    {
                                        case "DerivativeWithTime":
                                            if (value is INumericalType numerical)
                                            {
                                                object val = args.Parameters[0].Evaluate();
                                                switch (val)
                                                {
                                                    case BigInteger bigInteger:
                                                        args.Result = numerical.GetDerivate((int)bigInteger);
                                                        break;
                                                    default:
                                                        args.Result = numerical.GetDerivate((int)val);
                                                        break;
                                                }                                              
                                            }
                                            else
                                            {
                                                throw new ArgumentException("Cannot derive non numerical types");
                                            }
                                            break;
                                    }
                                };
                                e.EvaluateParameter += (string name, ParameterArgs args) =>
                                {
                                    switch (name)
                                    {
                                        case "x":
                                            args.Result = value.GetValueObject();
                                            break;
                                        case "derivative":
                                            if (value is INumericalType numerical)
                                            {
                                                var val = numerical.GetDerivate();
                                                switch (val)
                                                {
                                                    case BigInteger bigInteger:
                                                        args.Result = (double)bigInteger;
                                                        break;
                                                    default:
                                                        args.Result = (double)val;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                throw new ArgumentException("Cannot derive non numerical types");
                                            }
                                            break;
                                        default:
                                            string []parts;
                                            if(name.Contains("_"))
                                            {
                                                parts = name.Split('_');
                                            }
                                            else
                                            {
                                                parts = new string[] { name };
                                            }
                                            if (Parent != null)
                                            {
                                                DataSymbol usedSymb = null;
                                                if (parts.Length > 2)
                                                {
                                                    string symbolname = parts[0];
                                                    parts = parts.Skip(1).ToArray();
                                                    foreach(DataSymbol s in Parent.Sibblings)
                                                    {
                                                        if(s.Name == symbolname)
                                                        {
                                                            usedSymb = s;
                                                        }
                                                    }
                                                    if(usedSymb == null) throw new ArgumentException(symbolname + " doesn't name a valid symbol in this message");
                                                }
                                                else
                                                {
                                                    usedSymb = Parent;
                                                }
                                                foreach (DataEntry entry in usedSymb)
                                                {
                                                    if (entry.Name == parts[0])
                                                    {
                                                        if(parts.Length > 1)
                                                        {
                                                            switch(parts[1])
                                                            {
                                                                case "diff":
                                                                    if(entry.Value is INumericalType numericalType)
                                                                    {
                                                                        var val = numericalType.GetChange();
                                                                        switch (val)
                                                                        {
                                                                            case BigInteger bigInteger:
                                                                                args.Result = (double)bigInteger;
                                                                                break;
                                                                            default:
                                                                                args.Result = (double)val;
                                                                                break;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        throw new ArgumentException("Cannot find diff of non numerical value");
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if(entry.Value is INumericalType numericalType)
                                                            {
                                                                var val = numericalType.GetValueObject();
                                                                switch (val)
                                                                {
                                                                    case BigInteger bigInteger:
                                                                        args.Result = (double)bigInteger;
                                                                        break;
                                                                    default:
                                                                        args.Result = (double)val;
                                                                        break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                args.Result = entry.Value.GetValueObject();
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                };
                                return e.Evaluate();
                            };
                            break;
                        case "unit":
                            Unit = childNode.FirstChild.Value;
                            break;
                    }
                }
            }
            if(HasHistory)
            {
                type = new IType[historylength];
                ResetHistory();
            }
        }

        protected void ResetHistory()
        {
            for (int i = 0; i < type.Length; i++)
            {
                type[i] = Value.Copy();
            }
            historyindex = 0;
        }

        private void RollArray(IType[] arr)
        {
            for(int i = (arr.Length - 1); i > 0; i--)
            {
                arr[i] = arr[i - 1];
            }
            arr[0] = Value.Copy();
        }

        public bool HasHistory { get; private set; } = false;

        public DataSymbol Parent { get; set; } = null;

        public string Unit { get; private set; } = "";

        public int SetValue(IEnumerable<byte> data)
        {
            if(HasHistory)
            {
                if(historyindex >= type.Length)
                {
                    switch (historytype)
                    {
                        case HistoryType.reset:
                            ResetHistory();
                            break;
                        case HistoryType.roll:
                            RollArray(type);
                            historyindex = 0;
                            break;
                        default:
                            ResetHistory();
                            break;
                    }
                }
                int ret = type[historyindex].SetValue(data as byte[] ?? data.ToArray());
                historyindex++;
                return ret;
            }
            else
                return Value.SetValue(data as byte[] ?? data.ToArray());
        }

        public void SetValue(object data)
        {
            Value.SetValue(data);
        }

        private object HandleObjRequest(IType entry)
        {
            if (converterFunction != null)
            {
                return converterFunction(entry);
            }
            return entry.GetValueObject();
        }

        public object GetValueObject()
        {
            if (HasHistory)
            {
                object[] arr = new object[type.Length];
                for(int i = 0; i < arr.Length; i++)
                {
                    arr[i] = HandleObjRequest(type[i]);
                }
                return arr;
            }
            else
            {
                return HandleObjRequest(Value);
            }
        }
    }
}
