﻿using libxcm;
using System;
using System.Xml;
using libxcmparse.DataObjects;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using libxcmparse;
using System.Threading;
using System.Xml.Schema;
using System.IO;
using CommandLine;
using libconnection;
using libconnection.Interfaces;
using libconnection.Interfaces.UDP;
using System.Threading.Tasks;
using System.Reflection;

namespace xcmparser
{
    class Options
    {
        [Option('f', "xcmfile", Required = true, HelpText = "The xcm file with the package definitions")]
        public string Xcmfile { get; set; }
        [Option('p', "pipe", Required = true, HelpText = "The pipe expression on how to receive the data")]
        public string Pipe { get; set; }
        [Option('o', "outputinterface", HelpText = "Interface definition for the output UDP Server eg: 127.0.0.1:9000", Default = "127.0.0.1:9000")]
        public string OutputInterface { get; set; }

        [Option('v', "verbose", HelpText = "Enables console data output")]
        public bool Verbose { get; set; } = false;
        [Option("noforward", HelpText = "Disables the json output except console output")]
        public bool NoForward { get; set; } = false;
        [Option("enableinterface", HelpText = "Enables a basic text interface that can be opened by pressing the t key in the console window")]
        public bool EnableInterace { get; set; } = false;
    }
    class Program
    {
        static int Main(string[] args)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            Options opts = null;
            bool optionsValid = false;
            Task<bool> receiveTask = null;

            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string workingDir = Directory.GetCurrentDirectory();
            int ret = 0;
            try
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(o => { optionsValid = true; opts = o; });

                if (optionsValid && opts != null)
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.Schemas.Add("http://www.w3schools.com", Path.Combine(assemblyDir, "xcm.xsd"));
                    settings.ValidationType = ValidationType.Schema;

                    XmlReader reader = XmlReader.Create(opts.Xcmfile, settings);
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
                    using var client = new UdpTransmitter(null, IPEndPoint.Parse(opts.OutputInterface));
                    using var pipe = new StreamPipe(opts.Pipe);
                    Console.WriteLine("Starting interfaces");
                    receiveTask = Task.Run(async () =>
                    {
                        XCMParserTokenizer tokenizer = new XCMParserTokenizer(doc);
                        //using TelegrafUDPStream stream = new TelegrafUDPStream(new IPEndPoint(IPAddress.Any, 3550), IPEndPoint.Parse(args[1]));
                        { 
                            pipe.StartService();
                            client.StartService();

                            Console.WriteLine("Upstream interface and downstream interface started");

                            var token = cts.Token;
                            while (!token.IsCancellationRequested)
                            {
                                try
                                {
                                    var msg = await pipe.ReadMessageAsync(token);
                                    lock (opts)
                                    {
                                        ProcessMessage(msg.Data, tokenizer, client, opts);
                                    }
                                }
                                catch (OperationCanceledException)
                                {

                                }
                                catch (AggregateException ex)
                                {
                                    if (!(ex.InnerException is TimeoutException))
                                    {
                                        Console.WriteLine("Unkown exception:");
                                        Console.WriteLine(ex);
                                        cts.Cancel();
                                        return false;
                                    }
                                }
                            }
                            return true;
                        }
                    });
                    XCMParserTokenizer tokenizer = new XCMParserTokenizer(doc);
                    var token = cts.Token;
                    bool verbosity = opts.Verbose;
                    Console.CancelKeyPress += (_, args) =>
                    {
                        args.Cancel = true;
                        cts.Cancel();
                        Thread.Sleep(1300);
                    };
                    Console.WriteLine("Startup successfull");
                    while (!token.IsCancellationRequested)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            switch (key)
                            {
                                case ConsoleKey.T:
                                    bool enableInterface = false;
                                    lock(opts)
                                    {
                                        enableInterface = opts.EnableInterace;
                                    }
                                    if (enableInterface)
                                    {
                                        lock (opts)
                                        {
                                            opts.Verbose = false;
                                        }
                                        CommandEditStateMachine(tokenizer, pipe);
                                        Console.Clear();
                                        lock (opts)
                                        {
                                            opts.Verbose = verbosity;
                                        }
                                    }
                                    break;
                            }
                        }
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    return -2;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unkown exception:");
                Console.WriteLine(ex);
                ret = -3;
            }
            cts?.Cancel();
            Console.WriteLine("Shutting down");
            receiveTask?.Wait(1000);
            return ret;
        }

        enum State
        {
            ShowInfo,
            EditCommand,
            EditEntry
        }

        static void CommandEditStateMachine(XCMParserTokenizer tokenizer, DataStream stream)
        {
            State state = State.ShowInfo;
            bool running = true;
            var commands = tokenizer.GetObjects<DataCommand>();
            IEnumerable<KeyValuePair<DataSymbol, DataEntry>> enrylist = null;
            DataCommand selectedCommand = null;
            KeyValuePair<DataSymbol, DataEntry> selectedEntry = new KeyValuePair<DataSymbol, DataEntry>(null, null);
            try
            {
                while (running)
                {
                    Console.Clear();
                    switch (state)
                    {
                        case State.ShowInfo:
                            ResetConsole();
                            int commandCount = 0;
                            foreach (var command in commands)
                            {
                                Console.WriteLine($"{commandCount}\t{command.Name}");
                                commandCount++;
                            }
                            try
                            {
                                (var ret, var str) = HandleConsoleInput();
                                if (ret)
                                {
                                    running = false;
                                    break;
                                }
                                var commandNumber = uint.Parse(str);
                                selectedCommand = commands.Skip((int)commandNumber).First();
                                enrylist = GetCommandEntries(selectedCommand);
                                state = State.EditCommand;
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Invalid command number.");
                                Console.ReadKey();
                            }
                            catch (InvalidOperationException)
                            {
                                Console.WriteLine("Invalid command number.");
                                Console.ReadKey();
                            }
                            break;
                        case State.EditCommand:
                            commandCount = 0;
                            foreach (var entry in enrylist)
                            {
                                Console.WriteLine($"{selectedCommand.Name}->{entry.Key.Name}:");
                                Console.WriteLine($"{commandCount}:\t{entry.Value.Name} {entry.Value.Value}");
                                commandCount++;
                            }
                            try
                            {
                                (var ret, var str) = HandleConsoleInput();
                                if (ret)
                                {
                                    state = State.ShowInfo;
                                    break;
                                }
                                if (str == "")
                                {
                                    //Send the command and return
                                    state = State.EditCommand;
                                    var data = selectedCommand.GetData();
                                    stream.PublishDownstreamData(new libconnection.Message(data));
                                }
                                else
                                {
                                    var commandNumber = uint.Parse(str);
                                    selectedEntry = enrylist.Skip((int)commandNumber).First();
                                    state = State.EditEntry;
                                }
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Invalid command number.");
                                Console.ReadKey();
                            }
                            catch (InvalidOperationException)
                            {
                                Console.WriteLine("Invalid command number.");
                                Console.ReadKey();
                            }
                            break;
                        case State.EditEntry:
                            try
                            {
                                Console.WriteLine($"{selectedCommand.Name}->{selectedEntry.Key.Name}->{selectedEntry.Value.Name}: {selectedEntry.Value.Value}");
                                Console.WriteLine("Enter the new value:");
                                (var ret, var str) = HandleConsoleInput();
                                if (ret)
                                {
                                    state = State.EditCommand;
                                    break;
                                }
                                selectedEntry.Value.Value.SetValue(str);
                                state = State.EditCommand;
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Invalid data number.");
                                Console.ReadKey();
                            }
                            catch (InvalidOperationException)
                            {
                                Console.WriteLine("Invalid data number.");
                                Console.ReadKey();
                            }
                            break;
                    }
                }
            }
            catch(OperationCanceledException)
            {

            }
        }

        static (bool, string) HandleConsoleInput()
        {
            object line = Console.ReadLine();
            if (line != null)
            {
                var str = ((string)line).ToLower();
                bool ret = str == "e" || str == "exit";
                return (ret, str);
            }
            throw new OperationCanceledException();
        }

        static void ResetConsole()
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine("Type a number for the command you want to send and hit enter to set the values.");
            Console.WriteLine("Type e or exit to return.");
            Console.WriteLine();
        }

        static IEnumerable<KeyValuePair<DataSymbol, DataEntry>> GetCommandEntries(DataCommand msg)
        {
            foreach(DataSymbol symbol in msg)
            {
                foreach(DataEntry entry in symbol)
                {
                    yield return new KeyValuePair<DataSymbol, DataEntry>(symbol, entry);
                }
            }
        }

        static bool ProcessMessage(IEnumerable<byte> data, XCMParserTokenizer tokenizer, TelegrafUDPStream stream)
        {
            bool ret = false;
            foreach (var msg in tokenizer.GetObjects<DataMessage>())
            {
                var matcher = msg.GetMatchFunction();
                if (matcher(data))
                {
                    msg.ParseMessage(data.Skip(msg.IDByteLength + msg.IDOffset));
                    ret = true;
                    Console.WriteLine(TelegrafUDPStream.ConvertDataToJSON(msg));
                    stream.SendData(msg);
                }
            }
            return ret;
        }

        static bool ProcessMessage(IEnumerable<byte> data, XCMParserTokenizer tokenizer, DataStream stream, Options options = null)
        {
            bool ret = false;
            foreach (var msg in tokenizer.GetObjects<DataMessage>())
            {
                var matcher = msg.GetMatchFunction();
                if (matcher(data))
                {
                    msg.ParseMessage(data.Skip(msg.IDByteLength + msg.IDOffset));
                    ret = true;

                    if (options != null && options.Verbose)
                    {
                        var json = TelegrafUDPStream.ConvertDataToJSON(msg, true);
                        Console.WriteLine(json);
                    }
                    if (options == null || !options.NoForward)
                    {
                        var json = TelegrafUDPStream.ConvertDataToJSON(msg);
                        stream.PublishUpstreamData(new libconnection.Message(Encoding.UTF8.GetBytes(json)));
                    }
                }
            }
            return ret;
        }
    }
}
