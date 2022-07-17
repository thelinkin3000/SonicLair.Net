using SonicLair.Cli;

using SonicLairCli;

using Terminal.Gui;

var cosito = Environment.GetCommandLineArgs();
if (cosito.Length > 0 && cosito.Contains("-h"))
{
    var headless = new Headless();
    var token = headless.Token;
    var role = "standalone";
    if (cosito.Contains("-m"))
    {
        role = "master";
    }
    if (cosito.Contains("-s"))
    {
        role = "slave";
    }
    headless.Configure(role);
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