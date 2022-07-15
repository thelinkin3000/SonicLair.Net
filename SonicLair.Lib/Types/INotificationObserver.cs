using System.Collections.Generic;

namespace SonicLair.Lib.Types
{
    public interface INotificationObserver
    {
        void Update(string action, string value = null);
    }

    public interface INotifier
    {
        List<INotificationObserver> Observers { get; set; }

        void RegisterObserver(INotificationObserver observer);

        void UnregisterObserver(INotificationObserver observer);

        void NotifyObservers(string action, string value = null);
    }
}