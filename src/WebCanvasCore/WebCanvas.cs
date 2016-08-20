using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using Microsoft.AspNetCore.Hosting;

namespace WebCanvasCore
{
    enum MessageKey
    {
        DrawRect = 1,
        DrawLine = 2,
        SetFillStyle = 3,
        MousePos = 4,
        SetSize = 5,
        SetStrokeStyle = 6,
    }

    public class Vector2f
    {
        public float X { get; internal set; }
        public float Y { get; internal set; }

        public Vector2f(float x, float y)
        {
            X = x; Y = y;
        }
    }

    public class WebCanvas
    {
        private WebSocketMessageCenter _browserChannel;

        private IWebHost _host;

        private Thread _serverThread;

        private List<string> _messageBatch;

        private bool BatchUpdateInProgress => _messageBatch != null;        

        private string _lastMessageReceived;

        private Vector2f _mousePosition = new Vector2f(0, 0);

        public WebCanvas(string canvasHtmlPagePath, int port = 8442)
        {
            _browserChannel = new WebSocketMessageCenter();
            _browserChannel.OnMessageReceived = HandleMessageFromBrowser;            
            _browserChannel.OnWebSocketInitialized = () =>
            {
            };

            string html = File.ReadAllText(canvasHtmlPagePath);
            html = html.Replace("{%PORT%}", $"{port}");

            Startup.MessageCenter = _browserChannel;
            Startup.CanvasPageHtml = html;

            _host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:{port}")
                .UseStartup<Startup>()
                .Build();

            _serverThread = new Thread(_host.Run);
            _serverThread.Start();
        }

        public void Shutdown()
        {
            if (_host == null) return;

            _browserChannel.Close();
            _host.Dispose();

            _serverThread.Join();
        }

        private void SendMessageToBrowser(string message)
        {
            _browserChannel.SendMessage(message);
        }

        private void HandleMessageFromBrowser(string message)
        {
            _lastMessageReceived = message;
        }

        private void SendMessageToBrowserOrAddToBatch(string message)
        {
            if (BatchUpdateInProgress) _messageBatch.Add(message);
            else SendMessageToBrowser(message);
        }

        public void SetFillStyle(string style)
        {
            int messageKey = (int) MessageKey.SetFillStyle;
            string message = $"{messageKey},{style}";

            SendMessageToBrowserOrAddToBatch(message);
        }

        public void SetStrokeStyle(string style)
        {
            int messageKey = (int) MessageKey.SetStrokeStyle;
            string message = $"{messageKey},{style}";

            SendMessageToBrowserOrAddToBatch(message);
        }

        /// <summary>
        /// Draws a line of the specified width from point (xPointA, yPointA) to point (xPointB, yPointB).
        /// </summary>
        public void DrawLine(
            float xPointA, float yPointA,
            float xPointB, float yPointB,
            float width)
        {
            int messageKey = (int) MessageKey.DrawLine;
            string message = $"{messageKey},{xPointA},{yPointA},{xPointB},{yPointB},{width}";

            SendMessageToBrowserOrAddToBatch(message);
        }

        public void DrawRect(float x, float y, float width, float height)
        {
            int messageKey = (int) MessageKey.DrawRect;
            string message = $"{messageKey},{x},{y},{width},{height}";

            SendMessageToBrowserOrAddToBatch(message);
        }

        public void StartUpdateBatch()
        {
            _messageBatch = new List<string>();
        }

        public void ApplyUpdateBatch()
        {
            string message = string.Join("|", _messageBatch);

            SendMessageToBrowser(message);
            
            _messageBatch = null;
        }

        public Vector2f MousePosition
        {
            get
            {
                string message = _lastMessageReceived;

                bool messageOk = message != null
                    && message.Length > 0 // todo: fix
                    && (int)char.GetNumericValue(message[0]) == (int)MessageKey.MousePos;

                if (!messageOk) return _mousePosition;

                string[] components = _lastMessageReceived.Split(',');
                _mousePosition.X = float.Parse(components[1]);
                _mousePosition.Y = float.Parse(components[2]);

                return _mousePosition;
            }
        }

    }
}