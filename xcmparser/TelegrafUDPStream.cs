using libxcm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace xcmparser
{
    public class BigIntegerConverter : JsonConverter<BigInteger>
    {
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number)
                throw new JsonException(string.Format("Found token {0} but expected token {1}", reader.TokenType, JsonTokenType.Number));
            using var doc = JsonDocument.ParseValue(ref reader);
            return BigInteger.Parse(doc.RootElement.GetRawText(), NumberFormatInfo.InvariantInfo);
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            var s = value.ToString(NumberFormatInfo.InvariantInfo);
            using var doc = JsonDocument.Parse(s);
            doc.WriteTo(writer);
        }
    }
    internal class TelegrafUDPStream : IDisposable
    {

        private readonly UdpClient client;
        private Task task;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private ConcurrentQueue<byte[]> receivedData = new ConcurrentQueue<byte[]>();
        private bool isDisposed = false;

        public event EventHandler ReceivedData;
        public TelegrafUDPStream(IPEndPoint Receiveendpoint, IPEndPoint transmitEndpoint)
        {
            client = new UdpClient(Receiveendpoint);
            client.Connect(transmitEndpoint);
            task = Task.Factory.StartNew(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var receiveTask = client.ReceiveAsync();
                        var timer = Task.Delay(500);

                        var finishedTask = await Task.WhenAny(receiveTask, timer);
                        if (finishedTask == receiveTask)
                        {
                            var result = receiveTask.Result;
                            receivedData.Enqueue(result.Buffer);
                            ReceivedData?.Invoke(this, new EventArgs());
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        ~TelegrafUDPStream()
        {
            Dispose();
        }

        public byte[] ReadMessage()
        {
            byte[] ret;
            if(receivedData.TryDequeue(out ret))
            {
                return ret;
            }
            else
            {
                return new byte[0];
            }
        }

        public async Task<byte[]> ReadMessageAsync()
        {
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
            EventHandler f = (sender, args) =>
            {
                tcs.SetResult(ReadMessage());
            };
            ReceivedData += f;
            byte[] ret = await tcs.Task;
            ReceivedData -= f;
            return ret;
        }

        private static object SymbolToTagedObject(Symbol symb)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>();
            foreach(var entry in symb)
            {
                var val = entry.GetValue<object>();
                string name = entry.Name;
                switch (val)
                {
                    case bool:
                        name += "_bool";
                        break;
                    case string:
                        name += "_string";
                        break;
                    default:
                        break;
                }
                tags.Add(name, val);
            }
            return tags;
        }

        public static byte[] ConvertDataToJSONByte(Message msg)
        {
            return Encoding.UTF8.GetBytes(ConvertDataToJSON(msg));
        }
        public static string ConvertDataToJSON(Message msg, bool pretty = false)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>();
            foreach (var symbol in msg)
            {
                string name = symbol.Name;
                int counter = 0;
                if(string.IsNullOrWhiteSpace(name))
                {
                    do
                    {
                        name = "Anonymous" + counter;
                        counter++;
                    }while(tags.ContainsKey(name));
                }
                tags.Add(name, SymbolToTagedObject(symbol));
            }
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new BigIntegerConverter());
            options.WriteIndented = pretty;
            return JsonSerializer.Serialize(new
            {
                MessageName = msg.Name,
                Fields = tags
            },options);
        }

        public void SendData(Message msg)
        {
            byte[] data = ConvertDataToJSONByte(msg);
            client.Send(data, data.Length);
        }

        public async Task SendDataAsync(Message msg)
        {
            await Task.Run(() =>
            {
                SendData(msg);
            });
        }

        public void Dispose()
        {
            if(!isDisposed)
            {
                cts?.Cancel();
                task?.Wait(2000);
                try
                {
                    client?.Close();
                }
                catch (Exception)
                {

                }
                isDisposed = true;
            }
            task?.Dispose();
            cts?.Dispose();
            client?.Dispose();
        }
    }
}
