using Terminal.Gui;

namespace SonicLairCli
{
    public static class SonicLairControls
    {
        public static readonly ColorScheme InputColorScheme =
            new()
            {
                Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.White, Color.Black)
            };
            
        public static readonly ColorScheme ListViewColorScheme = new()
        {
            Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.Black, Color.White),
            HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Black),
        };
        
        public static TextField GetTextField(string defValue)
        {
            return new TextField(defValue)
            {
                ColorScheme = SonicLairControls.InputColorScheme
            };
        }

        public static void AnimateTextView(TextView field, string[] values, int delay, CancellationToken token)
        {
            _ = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var value in values)
                    {
                        Application.MainLoop.Invoke(() =>
                        {
                            field.Text = value;
                        });
                        Thread.Sleep(delay);
                    }
                }
            });
        }
    }
}