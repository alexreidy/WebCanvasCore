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
            {
                running = false;
            };

            WebCanvas c = WebCanvas.InitUsingDefaultHtmlPage(8442, onReadyToRender: canvas =>
            {
                var rng = new Random();

                int WIDTH = 1000, HEIGHT = 800;
                canvas.SetSize(WIDTH, HEIGHT);                

                while (running && canvas.CanRender)
                {
                    canvas.StartUpdateBatch();

                    canvas.SetFillStyle("green");
                    canvas.DrawRect(0, 0, WIDTH, HEIGHT);

                    IVector2f mpos = canvas.MousePosition;

                    if (canvas.MouseIsDown)
                    {
                        if (canvas.KeyIsPressed(KeyboardKey.Space)) canvas.SetStrokeStyle("cyan");
                        else canvas.SetStrokeStyle("lime");

                        for (int i = 0; i < 400; i++)
                        {
                            float x = (float) rng.NextDouble() * (float)WIDTH;
                            float y = (float) rng.NextDouble() * (float)HEIGHT;
                            
                            canvas.DrawLine(x, y, mpos.X-(mpos.X-x)*0.9f, mpos.Y-(mpos.Y-y)*0.9f, 2);
                        }
                    }

                    canvas.ApplyUpdateBatch();
                    Thread.Sleep(10);
                }

                if (!canvas.CanRender)
                    Console.WriteLine("This probably means no browser is connected right now");
            });
            
        }
    }
}
