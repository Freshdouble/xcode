using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace xcmparser
{
    internal class WSDataStream : WebSocketBehavior
    {
        private WebSocketStream ws = null;
        protected override void OnOpen()
        {
            Console.WriteLine($"Websocket connection established: {ID}");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            ws?.DisconnectWebSocket(this);
            Console.WriteLine($"Websocket disconnected: {ID}");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine($"Websocket error on: {ID}");
            Console.WriteLine($"Error Message: {e.Message}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
        }
        public void ConnectTo(WebSocketStream ws)
        {
            this.ws = ws;
            ws.ConnectWebSocket(this);
        }

        public new void Send(string data)
        {
            base.Send(data);
        }
    }
    internal class WebSocketStream : IDisposable
    {
        private readonly WebSocketServer ws;
        private readonly List<WSDataStream> wsStreams = new ();
        private readonly Mutex streamListMutex = new ();
        public WebSocketStream(IPEndPoint endpoint)
        {
            ws = new (endpoint.Address, endpoint.Port);
            ws.AddWebSocketService<WSDataStream>("/data", (s) =>
            {
                s.ConnectTo(this);
            });
            ws.Start();
        }

        public void ConnectWebSocket(WSDataStream ws)
        {
            streamListMutex.WaitOne();
            try
            {
                if (ws != null && !wsStreams.Contains(ws))
                {
                    wsStreams.Add(ws);
                }
            }
            finally
            {
                streamListMutex.ReleaseMutex();
            }
        }

        public void DisconnectWebSocket(WSDataStream ws)
        {
            streamListMutex.WaitOne();
            try
            {
                wsStreams.Remove(ws);
            }
            finally { streamListMutex.ReleaseMutex(); }
        }

        public void Send(string data)
        {
            var removeList = new List<WSDataStream>();
            streamListMutex.WaitOne();
            try
            {
                foreach (var stream in wsStreams)
                {
                    if (stream.State == WebSocketState.Open)
                    {
                        try
                        {
                            stream.Send(data);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Websocket sending failed: {ex.Message}");
                        }
                    }
                    else if (stream.State == WebSocketState.Closed)
                    {
                        removeList.Add(stream);
                    }
                }
                foreach (var stream in removeList)
                {
                    DisconnectWebSocket(stream);
                }
            }
            finally
            {
                streamListMutex.ReleaseMutex();
            }
        }

        public void Stop()
        {
            ws.Stop();
        }
        public void Dispose()
        {
            Stop();
        }
    }
}
