using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using Microsoft.AspNetCore.Hosting;

namespace WebCanvasCore
{
    enum MessageKey
    {
        KeyboardStateChanged = 1,
        MouseClickStateChanged = 2,
        MousePos = 3,

        SetCanvasSize = 4,
        SetFillStyle = 5,
        SetStrokeStyle = 6,        
        DrawRect = 7,
        DrawLine = 8,
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

        private Vector2f _mousepos = new Vector2f(0, 0);

        // The index represents the key code in KeyboardKey.
        // If the key is pressed, the element value is true.
        private bool[] _keyIsPressed = new bool[Enum.GetNames(typeof(KeyboardKey)).Length];

        public WebCanvas(string canvasHtmlPagePath, int port = 8442)
        {
            _browserChannel = new WebSocketMessageCenter();
            _browserChannel.OnMessageReceived = HandleMessageFromBrowser;            
            _browserChannel.OnWebSocketInitialized = () =>
            {
                OnReady();
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

        // todo: prevent direct instantiation and pass WebCanvas object into user-supplied
        // OnReady action in a factory method?
        public Action OnReady { get; set; } = () => {};

        public void Shutdown()
        {
            _browserChannel?.Close();
            _host?.Dispose();
            _serverThread?.Join();
        }

        private void SendMessageToBrowser(string message)
        {
            _browserChannel.SendMessage(message);
        }

        public void SetSize(int width, int height)
        {
            int messageKey = (int)MessageKey.SetCanvasSize;
            SendMessageToBrowser($"{messageKey},{width},{height}");
        }

        private void HandleMessageFromBrowser(string message)
        {
            // todo: is validating messages worthwhile here?

            bool messageOk = message != null && message.Length > 0;
            if (!messageOk) return;

            string[] components = message.Split(',');

            int messageKey;
            if (!int.TryParse(components[0], out messageKey)) return;

            switch (messageKey)
            {
                case (int)MessageKey.MousePos:
                    _mousepos.X = float.Parse(components[1]);
                    _mousepos.Y = float.Parse(components[2]);
                    break;

                case (int)MessageKey.KeyboardStateChanged:
                    int keyCode = int.Parse(components[1]);

                    bool keyCodeOk = keyCode >= 0 && keyCode < _keyIsPressed.Length;
                    if (!keyCodeOk) break;

                    _keyIsPressed[keyCode] = int.Parse(components[2]) == 1;

                    break;

                case (int)MessageKey.MouseClickStateChanged:
                    MouseIsDown = int.Parse(components[1]) == 1;
                    break;
            }
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

        public Vector2f MousePosition => _mousepos;

        public bool MouseIsDown { get; private set; }

        public bool KeyIsPressed(KeyboardKey key)
        {
            return _keyIsPressed[(int) key];
        }

    }
}