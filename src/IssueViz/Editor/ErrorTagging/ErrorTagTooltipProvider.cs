/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging
{
    /// <summary>
    /// Creates a tooltip object for an IErrorTag
    /// </summary>
    internal interface IErrorTagTooltipProvider
    {
        object Create(IAnalysisIssueBase analysisIssueBase);
    }

    [Export(typeof(IErrorTagTooltipProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ErrorTagTooltipProvider : IErrorTagTooltipProvider
    {
        private readonly IVsThemeColorProvider vsThemeColorProvider;
        private readonly INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;

        [ImportingConstructor]
        public ErrorTagTooltipProvider(IVsThemeColorProvider vsThemeColorProvider, INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand)
        {
            this.vsThemeColorProvider = vsThemeColorProvider;
            this.navigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
        }

        public object Create(IAnalysisIssueBase analysisIssueBase)
        {
            var hyperLink = new Hyperlink
            {
                Inlines = {analysisIssueBase.RuleKey},
                Foreground = GetVsThemedColor(EnvironmentColors.ControlLinkTextColorKey),
                TextDecorations = null,
                Command = navigateToRuleDescriptionCommand,
                CommandParameter = analysisIssueBase.RuleKey
            };

            var content = new TextBlock
            {
                Inlines =
                {
                    hyperLink,
                    ": ",
                    analysisIssueBase.Message
                },
                Foreground = GetVsThemedColor(EnvironmentColors.SystemCaptionTextBrushKey)
            };


            return content;
        }

        private Brush GetVsThemedColor(ThemeResourceKey resourceKey)
        {
            var textColor = vsThemeColorProvider.GetVsThemedColor(resourceKey);
            var color = Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B);
            
            return new SolidColorBrush(color);
        }
    }
}
