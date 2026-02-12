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

using System.Windows.Documents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Education.Controls
{
    public class RuleHelpUserControlViewModel : ViewModelBase
    {
        private FlowDocument document;
        private bool isShowRuleDescription;
        private bool isShowCannotShowRuleDescription;
        private bool isShowRuleDescriptionInBrowser;
        private SonarCompositeRuleId ruleId;

        public FlowDocument Document
        {
            get => document;
            private set => SetAndRaisePropertyChanged(ref document, value);
        }

        public SonarCompositeRuleId RuleId
        {
            get => ruleId;
            private set => SetAndRaisePropertyChanged(ref ruleId, value);
        }

        public bool IsShowPlaceholder => !IsShowRuleDescription && !IsShowCannotShowRuleDescription && !IsShowRuleDescriptionInBrowser;

        public bool IsShowRuleDescription
        {
            get => isShowRuleDescription;
            private set => SetAndRaisePropertyChanged(ref isShowRuleDescription, value);
        }

        public bool IsShowCannotShowRuleDescription
        {
            get => isShowCannotShowRuleDescription;
            private set => SetAndRaisePropertyChanged(ref isShowCannotShowRuleDescription, value);
        }

        public bool IsShowRuleDescriptionInBrowser
        {
            get => isShowRuleDescriptionInBrowser;
            private set => SetAndRaisePropertyChanged(ref isShowRuleDescriptionInBrowser, value);
        }

        public void ShowRuleDescription(FlowDocument flowDocument)
        {
            Document = flowDocument;
            RuleId = null;
            SetVisibilityState(showRuleDescription: true);
        }

        public void ShowCannotShowRuleDescription(SonarCompositeRuleId newRuleId)
        {
            RuleId = newRuleId;
            Document = null;
            SetVisibilityState(showCannotShowRuleDescription: true);
        }

        public void ShowRuleDescriptionInBrowser(SonarCompositeRuleId newRuleId)
        {
            RuleId = newRuleId;
            Document = null;
            SetVisibilityState(showRuleDescriptionInBrowser: true);
        }

        private void SetVisibilityState(
            bool showRuleDescription = false,
            bool showCannotShowRuleDescription = false,
            bool showRuleDescriptionInBrowser = false)
        {
            IsShowRuleDescription = showRuleDescription;
            IsShowCannotShowRuleDescription = showCannotShowRuleDescription;
            IsShowRuleDescriptionInBrowser = showRuleDescriptionInBrowser;
            RaisePropertyChanged(nameof(IsShowPlaceholder));
        }
    }
}
