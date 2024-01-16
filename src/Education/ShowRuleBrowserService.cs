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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Education
{
    /// <summary>
    /// Shows rule help in the browser
    /// </summary>
    /// <remarks>This interface is only intended to be used internally by <see cref="Education"/> to
    /// handle cases where we can't show the rule help in the IDE.</remarks>
    internal interface IShowRuleInBrowser
    {
        /// <summary>
        /// Navigates to rules.sonarsource.com to show the help for the specified rule
        /// </summary>
        void ShowRuleDescription(SonarCompositeRuleId ruleId);
    }

    [Export(typeof(IShowRuleInBrowser))]
    internal class ShowRuleInBrowserService : IShowRuleInBrowser
    {
        private readonly IBrowserService vsBrowserService;
        private readonly IRuleHelpLinkProvider ruleHelpLinkProvider;

        [ImportingConstructor]
        public ShowRuleInBrowserService(IBrowserService vsBrowserService)
            : this(vsBrowserService, new RuleHelpLinkProvider())
        {
        }

        internal /* for testing */ ShowRuleInBrowserService(IBrowserService vsBrowserService,
            IRuleHelpLinkProvider ruleHelpLinkProvider)
        {
            this.vsBrowserService = vsBrowserService;
            this.ruleHelpLinkProvider = ruleHelpLinkProvider;
        }

        public void ShowRuleDescription(SonarCompositeRuleId ruleId)
        {
            var helpLink = ruleHelpLinkProvider.GetHelpLink(ruleId.ErrorListErrorCode);
            vsBrowserService.Navigate(helpLink);
        }
    }
}
