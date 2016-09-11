using System;
using System.IO;

namespace ConsoleApplication
{
    public class Build
    {
        public static void Main(string[] args)
        {
            string canvasPageHtml = File.ReadAllText("../src/canvas.html").Replace("\"", "\"\"");

            string canvasHtmlConstClassTemplatePath = "DefaultCanvasHtmlPage.cs";
            string canvasHtmlConstClassSrc = File.ReadAllText(canvasHtmlConstClassTemplatePath)
                .Replace("CANVAS_PAGE_HTML", canvasPageHtml);
            
            string canvasHtmlConstClassDestinationPath = "../src/WebCanvasCore/DefaultCanvasHtmlPage.cs";
            File.WriteAllText(canvasHtmlConstClassDestinationPath,
                canvasHtmlConstClassSrc);
        }
    }
}