using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.HelpCommands
{
    internal class ShowLogsCommand : VsCommandBase
    {
        private readonly IOutputWindowService outputWindowService;

        public ShowLogsCommand(IOutputWindowService outputWindowService)
        {
            this.outputWindowService = outputWindowService;
        }

        protected override void InvokeInternal()
        {
            outputWindowService.Show();
        }
    }
}
