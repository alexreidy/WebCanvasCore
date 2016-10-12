using System;
using System.Threading;
using WebCanvasCore;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool running = true;
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs a) =>
                running = false;

            // You don't have to worry about canvas going out of scope and being eaten by the GC.
            var canvas = new WebCanvas();

            canvas.RunServingDefaultHtmlPage(port: 8442, onReadyToRender: () =>
            {
                // If a browser disconnects then reconnects, onReadyToRender will be called again.
                // If you want your program to continue when nobody's looking, you can leave onReadyToRender
                // empty and just have canvas method calls in your update loop; they have no effect
                // unless a browser is connected.

                var rng = new Random();

                const int Width = 1000, Height = 800;
                canvas.SetSize(Width, Height);

                canvas.SetFont("Arial", sizeInPixels: 14);

                while (running && canvas.CanRender)
                {
                    canvas.StartUpdateBatch();

                    canvas.SetFillStyle("green");
                    canvas.DrawRect(0, 0, Width, Height);

                    IVector2f mpos = canvas.MousePosition;

                    canvas.SetFillStyle("yellow");
                    canvas.DrawText($"({mpos.X}, {mpos.Y})", mpos.X, mpos.Y);

                    if (canvas.MouseIsDown)
                    {
                        if (canvas.KeyIsPressed(KeyboardKey.Space)) canvas.SetStrokeStyle("cyan");
                        else canvas.SetStrokeStyle("lime");

                        for (int i = 0; i < 400; i++)
                        {
                            float x = (float)rng.NextDouble() * (float)Width;
                            float y = (float)rng.NextDouble() * (float)Height;
                            
                            canvas.DrawLine(x, y, mpos.X-(mpos.X-x)*0.9f, mpos.Y-(mpos.Y-y)*0.9f, 2);
                        }
                    }

                    canvas.ApplyUpdateBatch();
                    Thread.Sleep(10);
                }

                if (!canvas.CanRender)
                    Console.WriteLine("This probably means there's no browser connected right now");
            });
        }
    }
}