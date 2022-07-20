using SonicLair.Cli;

using SonicLairCli;

using Terminal.Gui;

var cosito = Environment.GetCommandLineArgs();
if (cosito.Length > 0 && cosito.Contains("-h"))
{
    Console.WriteLine("Headless mode!");
    var headless = new Headless();
    var token = headless.Token;
    _ = headless.Configure();
    while (!token.IsCancellationRequested)
    {
        Thread.Sleep(1000);
    }
}
else
{
    Application.Init();
    Colors.Base.Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black);

    var top = Application.Top;
    var mainWindow = new MainWindow(top);

    var loginWindow = new LoginWindow(top, mainWindow);
    loginWindow.Load();

    Application.Run();
    Application.Shutdown();
}