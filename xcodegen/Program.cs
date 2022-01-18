using libxcm;
using libxcm.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Scriban;
using System.Linq;
using Scriban.Syntax;
using System.Xml.Schema;
using System.Reflection;
using CommandLine;

namespace xcodegen
{
    class Options
    {
        [Option('f', "xcmfile", Required = true, HelpText = "The xcm file with the package definitions")]
        public string Xcmfile { get; set; }
        [Option('t', "templatefolder", Required = false, HelpText = "The folder that contains the templates")]
        public string TemplateFolder { get; set; } = string.Empty;
        [Option('o', "outputfolder", Required = false, HelpText = "The folder where to write the output files")]
        public string OutputFolder { get; set; } = string.Empty;
        [Option('n', "filename", Required = false, HelpText = "The basefilename (without extension) for the generated output files", Default = "xcode")]
        public string FileName { get; set; }
        [Option("swap", HelpText = "Swap messages and commands in the output")]
        public bool Swap { get; set; } = false;
    }
    class Program
    {
        static int Main(string[] args)
        {
            Options opts = null;
            bool optionsValid = false;
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string workingDir = Directory.GetCurrentDirectory();
            string xcmFileName = string.Empty;
            string templateFolder = Path.Combine(assemblyDir, "templates_cpp");
            string outputFolder = workingDir;
            try
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(o => { optionsValid = true; opts = o; });
                if (optionsValid && opts != null)
                {
                    xcmFileName = opts.Xcmfile;
                    if (!string.IsNullOrEmpty(opts.TemplateFolder))
                    {
                        templateFolder = opts.TemplateFolder;
                    }
                    if (!string.IsNullOrEmpty(opts.OutputFolder))
                    {
                        outputFolder = opts.OutputFolder;
                    }
                }
                else
                {
                    return -1;
                }

                if (!File.Exists(xcmFileName))
                {
                    Console.WriteLine("Couldn't read file: " + xcmFileName);
                    return -1;
                }
                if (!Directory.Exists(templateFolder))
                {
                    Console.WriteLine("Couldn't read folder: " + templateFolder);
                    return -1;
                }
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid option: " + e.Message);
                return -1;
            }

            //Main programm
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add("http://www.w3schools.com", Path.Combine(assemblyDir, "xcm.xsd"));
                settings.ValidationType = ValidationType.Schema;

                XmlReader reader = XmlReader.Create(xcmFileName, settings);
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(reader);
                }
                catch (XmlSchemaValidationException ex)
                {
                    Console.WriteLine("Error in the xcm document on line {0}:{1}", ex.LineNumber, ex.LinePosition);
                    Console.WriteLine(ex.Message);
                    return -1;
                }

                TypeGenerator typegenerator = new TypeGenerator(Path.Combine(assemblyDir, "cdtypes.xml"));
                XCMTokenizer tokenizer = new XCMTokenizer(doc);

                int maxmessagesize = 0;
                int maxcommandsize = 0;

                foreach (var message in tokenizer.GetObjects<Message>())
                {
                    if (message.GetMaximumByteLength() > maxmessagesize)
                    {
                        maxmessagesize = message.GetMaximumByteLength();
                    }
                }

                foreach (var cmd in tokenizer.GetObjects<Command>())
                {
                    if (cmd.GetMaximumByteLength() > maxcommandsize)
                    {
                        maxcommandsize = cmd.GetMaximumByteLength();
                    }
                }

                object messages;
                object commands;

                if(opts.Swap)
                {
                    commands = CreateTemplateObject(tokenizer.GetObjects<Message>().ToList(), typegenerator);
                    messages = CreateTemplateObject(tokenizer.GetObjects<Command>().Cast<Message>().ToList(), typegenerator);
                }
                else
                {
                    messages = CreateTemplateObject(tokenizer.GetObjects<Message>().ToList(), typegenerator);
                    commands = CreateTemplateObject(tokenizer.GetObjects<Command>().Cast<Message>().ToList(), typegenerator);
                }

                foreach (string filepath in Directory.GetFiles(templateFolder))
                {
                    if (Path.GetExtension(filepath) == ".sbntxt")
                    {
                        string filename = Path.GetFileName(filepath);
                        string extension = filename.Substring(filename.IndexOf('.'), filename.LastIndexOf('.') - filename.IndexOf('.'));
                        string outputFileName = opts.FileName + extension;
                        string template = filepath;
                        var tmp = Template.Parse(File.ReadAllText(template), template);
                        var parsed = tmp.Render(new
                        {
                            messages,
                            commands,
                            maxmessagesize,
                            maxcommandsize,
                            outputfilename = outputFileName,
                            currentdate = DateTime.Now
                        });
                        string f = Path.Combine(outputFolder, outputFileName);
                        File.WriteAllText(f, parsed);
                        Console.WriteLine("Generated file: " + f);
                    }
                    else
                    {
                        string f = Path.Combine(outputFolder, Path.GetFileName(filepath));
                        File.Copy(filepath, f);
                        Console.WriteLine("Copied file: " + f);
                    }
                }
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Generation failed with: " + ex.Message);
            }
            catch (ScriptRuntimeException ex)
            {
                Console.WriteLine("The template has an error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unkown Error: " + ex.ToString());
            }
            return -1;
        }

        static object CreateTemplateObject(List<Message> msg, TypeGenerator typegenerator)
        {
            List<object> messages = new List<object>();

            foreach (var m in msg)
            {
                //Parse Messages
                string msgname = m.Name;
                List<object> msgSymbols = new List<object>();
                foreach (var symb in m)
                {
                    List<object> symbentries = new List<object>();
                    bool symbolisBitfield = RequireBitfield(symb);
                    foreach (var entry in symb)
                    {
                        if (!entry.IsVirtual)
                        {
                            string typename = typegenerator.FindType(entry)?.datatypename;
                            if (!string.IsNullOrEmpty(typename))
                            {
                                symbentries.Add(new
                                {
                                    internaltype = entry.BaseType,
                                    name = entry.Name,
                                    defaultvalue = entry.Value,
                                    description = entry.DocuText,
                                    isvarlength = entry.IsVariableLength,
                                    bitlength = entry.Bitlength,
                                    typename,
                                    bitfield = symbolisBitfield,
                                    length = entry.MaxBitLength / 8
                                });
                            }
                            else
                            {
                                throw new ArgumentException("Unkown type: " + entry.ToString());
                            }
                        }
                    }
                    if (RequireBitfield(symb) && symb.IsAnonymous)
                    {
                        throw new NotSupportedException("Anonymous symbols are not allowed in " + msgname);
                    }
                    msgSymbols.Add(new
                    {
                        entries = symbentries,
                        description = symb.DocuText,
                        isref = !symb.IsAnonymous && !symb.Modified,
                        name = symb.Name,
                        length = symb.GetPaddedBitlength() / 8,
                        isvarlength = symb.IsVariableLength,
                        isbitfield = RequireBitfield(symb),
                        maxlength = symb.GetMaxBitlength() / 8,
                        modified = symb.Modified,
                        global = symb.IsGlobal,
                        anonymous = symb.IsAnonymous
                    });
                }
                messages.Add(new
                {
                    symbols = msgSymbols,
                    description = m.DocuText,
                    isvarlength = m.IsVariableLength,
                    idisstring = m.IdentifierIsString,
                    idbyte = m.Identifier,
                    idstring = m.GetIDString(),
                    name = msgname,
                    idbytelength = m.IDByteLength,
                    payloadlength = m.GetByteLengthWithoutID(),
                    totallength = m.GetByteLength(),
                    maxlength = m.GetMaximumByteLength()
                });
            }
            return messages;
        }

        static private bool TypeAllowsBitfield(Entry entry)
        {
            return !(entry.BaseType == "double" || entry.BaseType == "float");
        }

        static private bool RequireBitfield(Symbol symbol)
        {
            foreach (Entry entry in symbol)
            {
                if (entry.IsVirtual) //Skip virtual entries
                    continue;
                if (entry.Bitlength % 8 != 0)
                    return true;
            }
            return false;
        }
    }
}
