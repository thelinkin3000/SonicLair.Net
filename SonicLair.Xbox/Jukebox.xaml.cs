using QRCoder;

using SonicLairXbox.Infrastructure;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace SonicLairXbox
{
    /// <summary>
    /// Una página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class Jukebox : Page
    {
        public Jukebox()
        {
            this.InitializeComponent();
            var ip = StaticHelpers.GetLocalIp();
            _ = Load(ip);
            tb_ip.Text = ip;
        }

        public async Task Load(string ip)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode($"{ip}j", QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                foreach (var b in qrCodeData.GetRawData(QRCodeData.Compression.Uncompressed))
                {
                    Debug.WriteLine((int)b);
                }
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
    }
}