﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands
{
    internal interface INavigateToCodeLocationCommand : ICommand
    {
    }

    [Export(typeof(INavigateToCodeLocationCommand))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class NavigateToCodeLocationCommand : DelegateCommand, INavigateToCodeLocationCommand
    {
        [ImportingConstructor]
        public NavigateToCodeLocationCommand(ILocationNavigator locationNavigator)
            : base(parameter =>
            {
                var locationViz = (IAnalysisIssueLocationVisualization) parameter;
                locationNavigator.TryNavigate(locationViz);
            }, parameter => parameter is IAnalysisIssueLocationVisualization)
        {
        }
    }
}
