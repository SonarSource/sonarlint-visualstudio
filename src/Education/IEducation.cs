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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Commands;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Rules;
using SonarLint.VisualStudio.Infrastructure.VS;
using Microsoft.VisualStudio.Threading;

namespace SonarLint.VisualStudio.Education
{
    public interface IEducation
    {
        void ShowRuleDescription(Language language, string ruleKey);
    }

    [Export(typeof(IEducation))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class Education : IEducation
    {
        private readonly IToolWindowService toolWindowService;
        private readonly IRuleDescriptionToolWindow ruleDescriptionToolWindow;
        private readonly ILogger logger;
        private readonly IRuleHelpXamlBuilder ruleHelpXamlBuilder;
        private readonly IRuleMetadataProvider ruleMetadataProvider;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public Education(IToolWindowService toolWindowService, IRuleMetadataProvider ruleMetadataProvider, ILogger logger)
            : this(toolWindowService, ruleMetadataProvider, logger, new RuleHelpXamlBuilder(), ThreadHandling.Instance) { }

        internal /* for testing */ Education(IToolWindowService toolWindowService, IRuleMetadataProvider ruleMetadataProvider, ILogger logger,
                                             IRuleHelpXamlBuilder ruleHelpXamlBuilder, IThreadHandling threadHandling)
        {
            this.toolWindowService = toolWindowService;
            this.ruleHelpXamlBuilder = ruleHelpXamlBuilder;
            this.ruleMetadataProvider = ruleMetadataProvider;
            this.logger = logger;
            this.threadHandling = threadHandling;

            ruleDescriptionToolWindow = toolWindowService.GetToolWindow<RuleDescriptionToolWindow, IRuleDescriptionToolWindow>();
        }

        public void ShowRuleDescription(Language language, string ruleKey)
        {
            var ruleHelp = ruleMetadataProvider.GetRuleHelp(language, ruleKey);

            threadHandling.RunOnUIThread(() =>
            {
                var flowDocument = ruleHelpXamlBuilder.Create(ruleHelp);

                ruleDescriptionToolWindow.UpdateContent(flowDocument);

                try
                {
                    toolWindowService.Show(RuleDescriptionToolWindow.ToolWindowId);
                }
                catch (Exception ex) when (!Core.ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine(string.Format(Resources.ERR_RuleDescriptionToolWindow_Exception, ex));
                }
            }).Forget();
        }
    }
}

