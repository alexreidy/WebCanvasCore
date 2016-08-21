using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebCanvasCore
{
    internal class WebSocketMessageCenter
    {
        public Action<string> OnMessageReceived { get; set; } = (string s) => {};

        public Action OnWebSocketInitialized { get; set; } = () => {};

        private WebSocket Socket { get; set; }

        public async void SendMessage(string message)
        {
            if (Socket == null) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            await Socket.SendAsync(
                new ArraySegment<Byte>(data),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        public async Task HandleWebSocketRequest(HttpContext http, Func<Task> next)
        {
            if (http.WebSockets.IsWebSocketRequest)
            {
                if (Socket == null)
                {
                    Socket = await http.WebSockets.AcceptWebSocketAsync();
                    OnWebSocketInitialized();
                }

                while (Socket.State == WebSocketState.Open)
                {
                    var buffer = new ArraySegment<Byte>(new Byte[4096]);
                    var received = await Socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (received.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                        if (OnMessageReceived != null)
                        {
                            OnMessageReceived(message);
                        }
                    }
                }
            }
            else
            {
                await next();
            }
        }
        
        public void Close()
        {   
            Socket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

    }
}