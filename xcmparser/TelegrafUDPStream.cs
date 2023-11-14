using libxcm;
using libxcmparse.DataObjects;
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
    internal class TelegrafUDPStream : IDisposable
    {

        private readonly UdpClient client;
        private readonly Task task;
        private readonly CancellationTokenSource cts = new ();
        private readonly ConcurrentQueue<byte[]> receivedData = new ();
        private bool isDisposed = false;

        public event EventHandler ReceivedData;
        public TelegrafUDPStream(IPEndPoint Receiveendpoint, IPEndPoint transmitEndpoint)
        {
            client = new UdpClient(Receiveendpoint);
            client.Connect(transmitEndpoint);
            var token = cts.Token;
            task = Task.Factory.StartNew(async () =>
            {
                while (!token.IsCancellationRequested)
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
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        ~TelegrafUDPStream()
        {
            Dispose();
        }

        public byte[] ReadMessage()
        {
            if (receivedData.TryDequeue(out byte[] ret))
            {
                return ret;
            }
            else
            {
                return Array.Empty<byte>();
            }
        }

        public async Task<byte[]> ReadMessageAsync()
        {
            TaskCompletionSource<byte[]> tcs = new();
            void f(object sender, EventArgs args)
            {
                tcs.SetResult(ReadMessage());
            }
            ReceivedData += f;
            byte[] ret = await tcs.Task;
            ReceivedData -= f;
            return ret;
        }

        public void SendData(DataMessage msg)
        {
            string name = msg.Name;
            byte[] data = JsonConverter.ConvertDataToJSONByte(msg, ref name);
            client.Send(data, data.Length);
        }

        public async Task SendDataAsync(DataMessage msg)
        {
            await Task.Run(() =>
            {
                SendData(msg);
            });
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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
