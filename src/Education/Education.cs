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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Commands;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education
{
    [Export(typeof(IEducation))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class Education : IEducation
    {
        private readonly IToolWindowService toolWindowService;
        private readonly IRuleMetaDataProvider ruleMetadataProvider;
        private readonly IRuleHelpXamlBuilder ruleHelpXamlBuilder;
        private readonly IShowRuleInBrowser showRuleInBrowser;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        private IRuleHelpToolWindow ruleHelpToolWindow;

        [ImportingConstructor]
        public Education(IToolWindowService toolWindowService, IRuleMetaDataProvider ruleMetadataProvider, IShowRuleInBrowser showRuleInBrowser, ILogger logger, IRuleHelpXamlBuilder ruleHelpXamlBuilder)
            : this(toolWindowService,
                ruleMetadataProvider,
                showRuleInBrowser,
                logger,
                ruleHelpXamlBuilder,
                ThreadHandling.Instance) { }

        internal /* for testing */ Education(IToolWindowService toolWindowService,
            IRuleMetaDataProvider ruleMetadataProvider,
            IShowRuleInBrowser showRuleInBrowser,
            ILogger logger,
            IRuleHelpXamlBuilder ruleHelpXamlBuilder,
            IThreadHandling threadHandling)
        {
            this.toolWindowService = toolWindowService;
            this.ruleHelpXamlBuilder = ruleHelpXamlBuilder;
            this.ruleMetadataProvider = ruleMetadataProvider;
            this.showRuleInBrowser = showRuleInBrowser;
            this.logger = logger;
            this.threadHandling = threadHandling;
        }

        public void ShowRuleHelp(SonarCompositeRuleId ruleId, string issueContext)
        {
            ShowRuleHelpAsync(ruleId, issueContext, CancellationToken.None).Forget();
        }

        private async Task ShowRuleHelpAsync(SonarCompositeRuleId ruleId, string issueContext, CancellationToken token)
        {
            await threadHandling.SwitchToBackgroundThread();

            var ruleInfo = await ruleMetadataProvider.GetRuleInfoAsync(ruleId, token);

            await threadHandling.RunOnUIThread(() =>
            {
                if (ruleInfo == null)
                {
                    showRuleInBrowser.ShowRuleDescription(ruleId);
                }
                else
                {
                    ShowRuleInIde(ruleInfo, ruleId, issueContext);
                }
            });
        }

        private void ShowRuleInIde(IRuleInfo ruleInfo, SonarCompositeRuleId ruleId, string issueContext)
        {
            threadHandling.ThrowIfNotOnUIThread();

            // Lazily fetch the tool window from a UI thread
            if (ruleHelpToolWindow == null)
            {
                ruleHelpToolWindow = toolWindowService.GetToolWindow<RuleHelpToolWindow, IRuleHelpToolWindow>();
            }

            try
            {
                var flowDocument = ruleHelpXamlBuilder.Create(ruleInfo);

                ruleHelpToolWindow.UpdateContent(flowDocument);

                toolWindowService.Show(RuleHelpToolWindow.ToolWindowId);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(string.Format(Resources.ERR_RuleHelpToolWindow_Exception, ex));
                showRuleInBrowser.ShowRuleDescription(ruleId);
            }
        }
    }
}
