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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.DebugMenu;

[ExcludeFromCodeCoverage]
internal class RuleIdInputViewModel : ViewModelBase
{
    private string ruleId;
    private string validationMessage;

    public string RuleId
    {
        get => ruleId;
        set
        {
            ruleId = value;
            ValidateRuleId();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsValidRuleId));
            RaisePropertyChanged(nameof(ValidationMessage));
        }
    }

    public bool IsValidRuleId { get; set; }

    public string ValidationMessage => validationMessage;

    private void ValidateRuleId()
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            IsValidRuleId = false;
            validationMessage = string.Empty;
        }
        else if (!SonarCompositeRuleId.TryParse(ruleId, out _))
        {
            IsValidRuleId = false;
            validationMessage = "Invalid rule ID format. Expected format: 'repo:ruleKey' (e.g., 'csharpsquid:S1000')";
        }
        else
        {
            IsValidRuleId = true;
            validationMessage = null;
        }
    }
}
