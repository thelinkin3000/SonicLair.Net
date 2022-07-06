using SonicLair.Services;

using SonicLairXbox.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=234238

namespace SonicLairXbox
{
    /// <summary>
    /// Una página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class AccountPage : Page,INotificationObserver
    {
        private readonly ISubsonicService _client;
        private readonly Account _account;
        public AccountPage()
        {
            this.InitializeComponent();
            ((App)App.Current).RegisterObserver(this);
            btn_logout.KeyDown += Btn_logout_KeyDown;
            btn_logout.Click += Btn_logout_Click;
            _client = (ISubsonicService)((App)App.Current).Container.GetService(typeof(ISubsonicService));
            _account = _client.GetActiveAccount();
            tb_username.Text = _account.Username;
            tb_server.Text = $"on {_account.Url}";
            // For now
            tb_type.Text = string.IsNullOrWhiteSpace(_account.Type) ? "" : $"running {_account.Type}";
            tb_plaintext_warning.Text = _account.UsePlaintext ? "using plaintext password" : "";
        }

        private void Btn_logout_Click(object sender, RoutedEventArgs e)
        {
            _client.Logout();
            ((App)App.Current).NotifyObservers("LOGOUT");
        }

        private void Btn_logout_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Left)
            {
                ((App)App.Current).NotifyObservers("focusSidebar");
            }
        }

        public void Update(string action, string value = "")
        {
            if(action == "focusContent")
            {
                btn_logout.Focus(FocusState.Keyboard);
            }
        }
    }
}
