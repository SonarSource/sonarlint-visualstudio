/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl
{
    [ExcludeFromCodeCoverage]
    [Guid("bb3677d1-3b5c-45a3-8a3a-897108c3ba28")]
    public class IssueVisualizationToolWindow : ToolWindowPane
    {
        public IssueVisualizationToolWindow(IServiceProvider serviceProvider) : base(null)
        {
            Caption = "SonarLint Issue Visualization";

            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var selectionService = componentModel.GetService<IAnalysisIssueSelectionService>();

            //var viewModel = new IssueVisualizationViewModel(selectionService);
            Content = new IssueVisualizationControl();
        }
    }
}
