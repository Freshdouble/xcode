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

        public void SendData(DataMessage msg)
        {
            byte[] data = JsonConverter.ConvertDataToJSONByte(msg);
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
