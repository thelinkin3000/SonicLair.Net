namespace SonicLair.Lib.Types.SonicLair
{
    public class Account
    {
        public Account()
        {

        }
        public Account(string username, string password, string url, bool plaintext)
        {
            Username = username;
            Password = password;
            Url = url;
            UsePlaintext = plaintext;
        }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public bool UsePlaintext { get; set; }
    }
}
