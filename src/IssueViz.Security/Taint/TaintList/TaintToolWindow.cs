/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList
{
    [Guid(IssueListIds.TaintIdAsString)]
    public class TaintToolWindow : ToolWindowPane
    {
        public static readonly Guid ToolWindowId = IssueListIds.TaintId;

        private TaintIssuesControl control;

        public TaintToolWindow(IServiceProvider serviceProvider)
        {
            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;

            var viewModel = new TaintIssuesControlViewModel(
                componentModel.GetService<ITaintStore>(),
                componentModel.GetService<ILocationNavigator>(),
                componentModel.GetService<IActiveDocumentTracker>(),
                componentModel.GetService<IActiveDocumentLocator>(),
                componentModel.GetService<IShowInBrowserService>(),
                componentModel.GetService<ITelemetryManager>(),
                componentModel.GetService<IIssueSelectionService>(),
                componentModel.GetService<INavigateToDocumentationCommand>(),
                GetService(typeof(IMenuCommandService)) as IMenuCommandService,
                componentModel.GetService<ISonarQubeService>(),
                componentModel.GetService<INavigateToRuleDescriptionCommand>()
            );

            Initialize(viewModel);
        }

        internal /* for testing */ TaintToolWindow(ITaintIssuesControlViewModel viewModel) =>
            Initialize(viewModel);

        private void Initialize(ITaintIssuesControlViewModel viewModel)
        {
            control = new TaintIssuesControl(viewModel);
            control.ViewModel.PropertyChanged += OnPropertyChanged;
            Caption = control.ViewModel.WindowCaption;
            Content = control;
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ITaintIssuesControlViewModel.WindowCaption))
            {
                Caption = control.ViewModel.WindowCaption;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                control.ViewModel.PropertyChanged -= OnPropertyChanged;
            }

            base.Dispose(disposing);
        }
    }
}
