using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace WebCanvasCore
{
    internal class Startup
    {
        public static WebSocketMessageCenter MessageCenter { get; set; }

        public static string CanvasPageHtml { get; set; }

        public void Configure(IApplicationBuilder app)
        {
            if (MessageCenter == null)
            {
                throw new Exception($"{nameof(MessageCenter)} is required");
            }
            
            app.UseWebSockets();
            app.Use(MessageCenter.HandleWebSocketRequest);

            app.Run(async (http) =>
            {
                if (CanvasPageHtml == null)
                {
                    throw new Exception("{nameof(CanvasPageHtml)} is required");
                }

                await http.Response.WriteAsync(CanvasPageHtml);
            });
        }
    }
}