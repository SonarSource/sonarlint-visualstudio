/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.DebugMenu;

[ExcludeFromCodeCoverage]
internal class OpenRuleDescriptionCommand(IEducation educationService) : VsCommandBase
{
    public const int Id = 0x102C;
    private readonly IEducation educationService = educationService ?? throw new ArgumentNullException(nameof(educationService));

    protected override void InvokeInternal()
    {
        var dialog = new RuleIdInputDialog();
        if (dialog.ShowDialog(Application.Current.MainWindow) == true
            && SonarCompositeRuleId.TryParse(dialog.ViewModel.RuleId, out var ruleId))
        {
            educationService.ShowRuleHelp(ruleId, null);
        }
    }
}
