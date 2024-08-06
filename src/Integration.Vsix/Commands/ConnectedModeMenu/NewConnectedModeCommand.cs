using SonarLint.VisualStudio.ConnectedMode.Migration.Wizard;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class NewConnectedModeCommand : VsCommandBase
    {
        internal const int Id = 0x104;

        protected override void InvokeInternal()
        {
           new NewConnectedMode().ShowDialog();
        }
    }
}
