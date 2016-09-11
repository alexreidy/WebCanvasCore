using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using Microsoft.AspNetCore.Hosting;

namespace WebCanvasCore
{
    internal enum MessageKey
    {
        KeyboardStateChanged = 1,
        MouseClickStateChanged = 2,
        MousePosition = 3,

        SetCanvasSize = 4,
        SetFillStyle = 5,
        SetStrokeStyle = 6,        
        DrawRect = 7,
        DrawLine = 8,
    }

    public interface IVector2f
    {
        float X { get; }
        float Y { get; }
    }

    internal class Vector2f : IVector2f
    {
        public float X { get; set; }
        public float Y { get; set; }

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

        public bool BatchUpdateInProgress => _messageBatch != null;

        private Vector2f _mousePosition = new Vector2f(0, 0);

        // The index represents the key code in KeyboardKey.
        // If the key is pressed, the element value is true.
        private bool[] _keyIsPressed = new bool[Enum.GetNames(typeof(KeyboardKey)).Length];

        public bool CanRender { get; private set; }

        private WebCanvas(string canvasPageHtml, int port, Action<WebCanvas> onBrowserConnected, Action<WebCanvas> onBrowserDisconnected)
        {
            _browserChannel = new WebSocketMessageCenter();
            _browserChannel.OnMessageReceived = HandleMessageFromBrowser;
            _browserChannel.OnWebSocketOpen = () => { onBrowserConnected(this); };
            _browserChannel.OnWebSocketNoLongerOpen = () => { onBrowserDisconnected(this); };

            Startup.MessageCenter = _browserChannel;
            Startup.CanvasPageHtml = canvasPageHtml;

            _host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:{port}")
                .UseStartup<Startup>()
                .Build();

            _serverThread = new Thread(_host.Run);
            _serverThread.Start();
        }

        public static WebCanvas InitUsingHtmlPage(string html, int port, Action onReadyToRender)
        {
            html = html.Replace("{%PORT%}", $"{port}");
            
            return new WebCanvas(html, port,
                onBrowserConnected: canvas =>
                {
                    canvas.CanRender = true;
                    onReadyToRender();
                },
                onBrowserDisconnected: canvas =>
                {
                    canvas.CanRender = false;
                });
        }

        public static WebCanvas InitUsingHtmlPageAtPath(string htmlPagePath, int port, Action onReadyToRender)
        {
            string html = File.ReadAllText(htmlPagePath);
            return InitUsingHtmlPage(html, port, onReadyToRender);
        }

        public static WebCanvas InitUsingDefaultHtmlPage(int port, Action onReadyToRender)
        {
            return InitUsingHtmlPage(DefaultCanvasHtmlPage.Html, port, onReadyToRender);
        }

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

        private void SendMessageToBrowserOrAddToBatch(string message)
        {
            if (BatchUpdateInProgress) _messageBatch.Add(message);
            else SendMessageToBrowser(message);
        }

        private void HandleMessageFromBrowser(string message)
        {            
            string[] components = message.Split(',');

            int messageKey;
            if (!int.TryParse(components[0], out messageKey)) return;

            switch (messageKey)
            {
                case (int)MessageKey.MousePosition:
                    _mousePosition.X = float.Parse(components[1]);
                    _mousePosition.Y = float.Parse(components[2]);
                    break;

                case (int)MessageKey.KeyboardStateChanged:
                    int keyCode = int.Parse(components[1]);
                    _keyIsPressed[keyCode] = int.Parse(components[2]) == 1;
                    break;

                case (int)MessageKey.MouseClickStateChanged:
                    MouseIsDown = int.Parse(components[1]) == 1;
                    break;
            }
        }

        public void SetSize(int width, int height)
        {
            int messageKey = (int)MessageKey.SetCanvasSize;
            SendMessageToBrowser($"{messageKey},{width},{height}");
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
            int messageKey = (int)MessageKey.DrawLine;
            string message = $"{messageKey},{xPointA},{yPointA},{xPointB},{yPointB},{width}";

            SendMessageToBrowserOrAddToBatch(message);
        }

        public void DrawRect(float x, float y, float width, float height)
        {
            int messageKey = (int)MessageKey.DrawRect;
            string message = $"{messageKey},{x},{y},{width},{height}";

            SendMessageToBrowserOrAddToBatch(message);
        }

        public void StartUpdateBatch()
        {
            _messageBatch = new List<string>();
        }

        public void CancelUpdateBatch()
        {
            _messageBatch = null;
        }

        public void ApplyUpdateBatch()
        {
            string message = string.Join("|", _messageBatch);

            SendMessageToBrowser(message);
            
            _messageBatch = null;
        }

        public IVector2f MousePosition => _mousePosition;

        public bool MouseIsDown { get; private set; }

        public bool KeyIsPressed(KeyboardKey key)
        {
            return _keyIsPressed[(int)key];
        }

    }
}