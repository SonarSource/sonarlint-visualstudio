using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class IssueVisualizationToolWindowCommand
    {
        public static readonly Guid CommandSet = new Guid("FDEF405A-28C2-4AFD-A37B-49EF2B0D142E");
        public const int CommandId = 0x0100;

        public static IssueVisualizationToolWindowCommand Instance { get; private set; }

        private readonly AsyncPackage package;

        private IssueVisualizationToolWindowCommand(AsyncPackage package, IMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            Instance = new IssueVisualizationToolWindowCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = package.FindToolWindow(typeof(IssueVisualizationToolWindow), 0, true);

            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
