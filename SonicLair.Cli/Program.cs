using SonicLairCli;

using Terminal.Gui;

Application.Init();
Colors.Base.Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black);

var top = Application.Top;
var mainWindow = new MainWindow(top);

var loginWindow = new LoginWindow(top, mainWindow);
loginWindow.Load();


Application.Run();
Application.Shutdown();