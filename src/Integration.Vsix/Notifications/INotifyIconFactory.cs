namespace SonarLint.VisualStudio.Integration.Vsix.Notifications
{
    public interface INotifyIconFactory
    {
        INotifyIcon Create();
    }

    public class NotifyIconFactory : INotifyIconFactory
    {
        public INotifyIcon Create() => new NotifyIconWrapper();
    }
}
