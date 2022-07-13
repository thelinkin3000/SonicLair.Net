using Microsoft.Extensions.DependencyInjection;

using SonicLair.Services;

using SonicLairXbox.Infrastructure;
using SonicLairXbox.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SonicLairXbox
{
    /// <summary>
    /// Proporciona un comportamiento específico de la aplicación para complementar la clase Application predeterminada.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Inicializa el objeto de aplicación Singleton. Esta es la primera línea de código creado
        /// ejecutado y, como tal, es el equivalente lógico de main() o WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.RequiresPointerMode = Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;
            Container = ConfigureDependencyInjection();
            Observers = new List<INotificationObserver>();
            _ = Windows.UI.ViewManagement.ApplicationViewScaling.TrySetDisableLayoutScaling(true);
            WebSocketService = new WebSocketService();
        }
        private readonly WebSocketService WebSocketService;
        public IServiceProvider Container { get; }
        public List<INotificationObserver> Observers { get; set; }

        public void RegisterObserver(INotificationObserver observer)
        {
            Observers.Add(observer);
        }

        public void UnregisterObserver(INotificationObserver observer)
        {
            if (Observers.Contains(observer))
            {
                Observers.Remove(observer);
            }
        }

        public void NotifyObservers(string action, string value = null)
        {
            try
            {
                foreach(var observer in Observers)
                {
                    observer.Update(action, value);
                }

            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        IServiceProvider ConfigureDependencyInjection()
        {
            var serviceCollection= new ServiceCollection();

            serviceCollection.AddSingleton<ISubsonicService, SubsonicService>();
            serviceCollection.AddSingleton<IMusicPlayerService, MusicPlayerService>();
            return serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// Se invoca cuando la aplicación la inicia normalmente el usuario final. Se usarán otros puntos
        /// de entrada cuando la aplicación se inicie para abrir un archivo específico, por ejemplo.
        /// </summary>
        /// <param name="args">Información detallada acerca de la solicitud y el proceso de inicio.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // No repetir la inicialización de la aplicación si la ventana tiene contenido todavía,
            // solo asegurarse de que la ventana está activa.
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Crear un marco para que actúe como contexto de navegación y navegar a la primera página.
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;



                // Poner el marco en la ventana actual.
                Window.Current.Content = rootFrame;
            }

            if (!args.PrelaunchActivated)
            {
                if (rootFrame.Content == null)
                {
                    // Cuando no se restaura la pila de navegación, navegar a la primera página,
                    // configurando la nueva página pasándole la información requerida como
                    //parámetro de navegación
                    rootFrame.Navigate(typeof(MainPage), args.Arguments);
                }
                // Asegurarse de que la ventana actual está activa.
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Se invoca cuando la aplicación la inicia normalmente el usuario final. Se usarán otros puntos
        /// </summary>
        /// <param name="sender">Marco que produjo el error de navegación</param>
        /// <param name="e">Detalles sobre el error de navegación</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            StaticHelpers.ShowError("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Se invoca al suspender la ejecución de la aplicación. El estado de la aplicación se guarda
        /// sin saber si la aplicación se terminará o se reanudará con el contenido
        /// de la memoria aún intacto.
        /// </summary>
        /// <param name="sender">Origen de la solicitud de suspensión.</param>
        /// <param name="e">Detalles sobre la solicitud de suspensión.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            
            deferral.Complete();
        }
    }
}
