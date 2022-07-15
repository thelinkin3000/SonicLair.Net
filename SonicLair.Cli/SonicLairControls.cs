using Terminal.Gui;

namespace SonicLairCli
{
    public static class SonicLairControls
    {
        public static ColorScheme InputColorScheme = new ColorScheme() { Normal = Application.Driver.MakeAttribute(Color.BrightBlue, Color.Black), Focus = Application.Driver.MakeAttribute(Color.White, Color.Black) };
        public static TextField GetTextField(string defValue)
        {
            return new TextField(defValue)
            {
                ColorScheme = SonicLairControls.InputColorScheme
            };
        }
    }
}