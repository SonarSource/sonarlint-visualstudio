/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands
{
    public interface INavigateToRuleDescriptionCommand : ICommand
    {
    }

    [Export(typeof(INavigateToRuleDescriptionCommand))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class NavigateToRuleDescriptionCommand : DelegateCommand, INavigateToRuleDescriptionCommand
    {
        [ImportingConstructor]
        public NavigateToRuleDescriptionCommand(IEducation educationService)
            : base(parameter =>
                {
                    var fullRuleKey = (string)parameter;
                    if (SonarCompositeRuleId.TryParse(fullRuleKey, out var ruleId))
                    {
                        educationService.ShowRuleHelp(ruleId, /* todo */ null);
                    }
                },
                parameter => parameter is string s &&
                    !string.IsNullOrEmpty(s) &&
                SonarCompositeRuleId.TryParse(s, out var _))
        {
        }
    }
}
