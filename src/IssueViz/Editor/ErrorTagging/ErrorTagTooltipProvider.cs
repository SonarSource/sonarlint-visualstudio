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

using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
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
        private static readonly Uri dummyUri = new Uri("rules://sonarlint");
        private readonly IVsThemeColorProvider vsThemeColorProvider;
        private readonly INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
        private readonly ILogger logger;

        [ImportingConstructor]
        public ErrorTagTooltipProvider(IVsThemeColorProvider vsThemeColorProvider, INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand, ILogger logger)
        {
            this.vsThemeColorProvider = vsThemeColorProvider;
            this.navigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
            this.logger = logger;
        }

        private static long instanceCount = 0;

        public object Create(IAnalysisIssueBase analysisIssueBase)
        {
            // Note on click handling:
            // Hyperlinks support two ways of handling clicks
            // 1. normal XAML Command binding
            // 2. Url + RequestNavigate event
            // There is an observable difference in behaviour between the two in error tooltips.
            // Method (1) requires the user to click twice on the link before it is actioned.
            // Method (2) requires the user to click only once. We want the second behaviour.
            // See bug #3650: https://github.com/SonarSource/sonarlint-visualstudio/issues/3650

            var hyperLink = new Hyperlink
            {
                Inlines = { analysisIssueBase.RuleKey },
                Foreground = GetVsThemedColor(EnvironmentColors.ControlLinkTextColorKey),

                NavigateUri = dummyUri // must set something, otherwise the RequestNavigate event won't fire.
            };

            hyperLink.RequestNavigate += (sender, args) => navigateToRuleDescriptionCommand.Execute(new NavigateToRuleDescriptionCommandParam { FullRuleKey = analysisIssueBase.RuleKey });

            ApplyHyperlinkStyle(hyperLink);

            var content = new TextBlock
            {
                Inlines =
                {
                    hyperLink,
                    ": ",
                    analysisIssueBase.PrimaryLocation.Message
                },
                Foreground = GetVsThemedColor(EnvironmentColors.SystemCaptionTextBrushKey)
            };

            instanceCount++;
            logger.LogVerbose($"[ErrorTagTooltipProvider] tooltip instance count: {instanceCount}");

            return content;
        }

        private static void ApplyHyperlinkStyle(Hyperlink hyperlink)
        {
            // Style the hyperlink to behave like other error tooltips i.e.
            // link is not underlined by default; underline when the user
            // moves the mouse over the element
            hyperlink.Style = new Style
            {
                Setters =
                {
                    new Setter(Inline.TextDecorationsProperty, null)
                },
                Triggers = {
                    new Trigger()
                    {
                        Property = ContentElement.IsMouseOverProperty,
                        Value = true,
                        Setters =
                        {
                            new Setter(Inline.TextDecorationsProperty, TextDecorations.Underline)
                        }
                    }
                }
            };
        }

        private Brush GetVsThemedColor(ThemeResourceKey resourceKey)
        {
            var textColor = vsThemeColorProvider.GetVsThemedColor(resourceKey);
            var color = Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B);

            return new SolidColorBrush(color);
        }
    }
}
