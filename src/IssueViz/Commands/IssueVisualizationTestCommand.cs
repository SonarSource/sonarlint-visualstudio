using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class IssueVisualizationTestCommand
    {
        public static readonly Guid CommandSet = new Guid("FDEF405A-28C2-4AFD-A37B-49EF2B0D142E");
        public const int CommandId = 0x0101;

        public static IssueVisualizationTestCommand Instance { get; private set; }

        private readonly AsyncPackage package;

        private IssueVisualizationTestCommand(AsyncPackage package, IMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            Instance = new IssueVisualizationTestCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var componentModel = (package as IServiceProvider).GetService(typeof(SComponentModel)) as IComponentModel;
            var selectionService = componentModel.GetService<IAnalysisIssueSelectionService>();

            PopulateMockVisualization(selectionService);
        }

        private static void PopulateMockVisualization(IAnalysisIssueSelectionService selectionService)
        {
            var flow1Location1 = new AnalysisIssueLocation("message 1", "c:\\test.cpp", 1, 2, 3, 4);
            var flow1Location2 = new AnalysisIssueLocation("message 2", "c:\\test.cpp", 1, 2, 3, 4);
            var flow1 = new AnalysisIssueFlow(new[] { flow1Location1, flow1Location2 });

            var flow2Location1 = new AnalysisIssueLocation("message 3", "c:\\test.cpp", 1, 2, 3, 4);
            var flow2Location2 = new AnalysisIssueLocation("message 4", "c:\\test.cpp", 1, 2, 3, 4);
            var flow2 = new AnalysisIssueFlow(new[] { flow2Location1, flow2Location2 });

            var flows = new[] { flow1, flow2 };

            var issue = new AnalysisIssue("test rule", AnalysisIssueSeverity.Blocker, AnalysisIssueType.Bug,
                "this is a test", "c:\\test.cpp", 1, 2, 0, 0, flows);

            var issueVisualization = new AnalysisIssueVisualizationConverter(new LocationNavigationChecker()).Convert(issue);

            selectionService.SelectedIssue = issueVisualization;
        }
    }
}
