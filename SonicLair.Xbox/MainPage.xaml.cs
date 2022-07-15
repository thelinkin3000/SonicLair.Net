
using Newtonsoft.Json;

using QRCoder;

using SonicLair.Lib.Services;
using SonicLair.Lib.Types;
using SonicLair.Lib.Types.SonicLair;

using SonicLairXbox.Infrastructure;

using System;
using System.IO;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace SonicLairXbox
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotificationObserver
    {

        private readonly ISubsonicService _client;
        public FormData Model { get; set; }
        public MainPage()
        {
            this.InitializeComponent();
            _client = (ISubsonicService)((App)App.Current).Container.GetService(typeof(ISubsonicService));
            this.Model = new FormData()
            {
                Validator = i =>
                {
                    var u = i as FormData;
                    if (string.IsNullOrEmpty(u.Username))
                    {
                        u.Properties[nameof(u.Username)].Errors.Add("The username is required");
                    }
                    if (string.IsNullOrEmpty(u.Password))
                    {
                        u.Properties[nameof(u.Password)].Errors.Add("The password is required");
                    }
                    if (string.IsNullOrEmpty(u.Url))
                    {
                        u.Properties[nameof(u.Url)].Errors.Add("The url is required");
                    }
                }
            };
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            ((App)App.Current).RegisterObserver(this);
            if (localSettings.Containers.ContainsKey("soniclair") && localSettings.Containers["soniclair"].Values.ContainsKey("activeAccount"))
            {
                var activeAccount = JsonConvert.DeserializeObject<Account>(localSettings.Containers["soniclair"].Values["activeAccount"].ToString());
                _ = Login(activeAccount);
            }
            _ = Load();



        }

        private async Task Load()
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(StaticHelpers.GetLocalIp(), QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                var qrCodeImage = qrCode.GetGraphic(20,
                    new byte[] { (byte)40, (byte)44, (byte)52 },
                    new byte[] { (byte)255, (byte)255, (byte)255 });
                using (MemoryStream ms = new MemoryStream(qrCodeImage))
                {
                    var im = ms.AsRandomAccessStream();
                    var decoder = await BitmapDecoder.CreateAsync(im);
                    im.Seek(0);
                    var output = new WriteableBitmap((int)decoder.PixelHeight, (int)decoder.PixelWidth);
                    await output.SetSourceAsync(im);
                    img_qr.Source = output;
                }
            }
        }

        private async Task Login(Account account)
        {
            _client.Configure(account);
            try
            {
                _ = await _client.GetArtists();
                SaveAccount(account);
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Frame.Navigate(typeof(Layout));
                });
            }
            catch (SubsonicException ex)
            {
                StaticHelpers.ShowError(ex.Message);
            }
        }

        private void Btn_login_Click(object sender, RoutedEventArgs e)
        {
            if (Model.Validate())
            {
                _ = Login(new Account(Model.Username, Model.Password, Model.Url, Model.Plaintext));
            }
            else
            {
                tb_errors.Text = Model.ErrorsAsString;
            }
        }

        private void Btn_qr_Click(object sender, RoutedEventArgs e)
        {
            if (img_qr.Visibility == Visibility.Collapsed)
            {
                img_qr.Visibility = Visibility.Visible;
            }
            else
            {
                img_qr.Visibility = Visibility.Collapsed;
            }
        }

        public void SaveAccount(Account account)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (!localSettings.Containers.ContainsKey("soniclair"))
            {
                _ =
                   localSettings.CreateContainer("soniclair", Windows.Storage.ApplicationDataCreateDisposition.Always);
            }
            localSettings.Containers["soniclair"].Values["activeAccount"] = JsonConvert.SerializeObject(account);
        }

        public void Update(string action, string value = "")
        {
            if (action == "WSLOGIN")
            {
                try
                {
                    var account = JsonConvert.DeserializeObject<Account>(value);
                    _ = Login(account);
                }
                catch (Exception)
                {
                    // Ignore the exception for now
                }
            }
        }
    }

    public class FormData : SoniclairValidatable
    {
        public string Username { get { return Read<string>(); } set { Write(value); } }
        public string Password { get { return Read<string>(); } set { Write(value); } }
        public string Url { get { return Read<string>(); } set { Write(value); } }
        public bool Plaintext { get { return Read<bool>(); } set { Write(value); } }

    }

    public class SoniclairValidatable : Template10.Validation.ValidatableModelBase
    {
        public string ErrorsAsString => String.Join("\n", this.Errors);
    }
}
