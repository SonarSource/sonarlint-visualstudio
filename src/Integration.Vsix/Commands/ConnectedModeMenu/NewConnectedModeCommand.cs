using SonarLint.VisualStudio.ConnectedMode.Migration.Wizard;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class NewConnectedModeCommand : VsCommandBase
    {
        private readonly IBrowserService browserService;
        internal const int Id = 0x104;

        public NewConnectedModeCommand(IBrowserService browserService)
        {
            this.browserService = browserService;
        }

        protected override void InvokeInternal()
        {
           new NewConnectedMode(browserService).ShowDialog();
        }
    }
}
