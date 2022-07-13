namespace SonicLairXbox
{
    public interface INotificationObserver
    {
        void Update(string action, string value = null);
    }
}