# WebCanvasCore

WebCanvasCore lets you control a HTML5 Canvas element with C# and .NET Core.

Every update message to or from the browser is currently sent with TCP, so it's dramatically slower than native rendering,
but it should suffice for rendering simple visualizations and handling user input when the tolerance for lag is high.