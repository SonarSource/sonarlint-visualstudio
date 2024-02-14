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

using System;
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    /// <summary>
    /// Provides <see cref="IRichRuleDescription"/> from <see cref="IRuleInfo"/>
    /// </summary>
    internal interface IRichRuleDescriptionProvider
    {
        IRichRuleDescription GetRichRuleDescriptionModel(IRuleInfo ruleInfo);
    }

    [Export(typeof(IRichRuleDescriptionProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RichRuleDescriptionProvider : IRichRuleDescriptionProvider
    {
        public IRichRuleDescription GetRichRuleDescriptionModel(IRuleInfo ruleInfo) =>
            new RichRuleDescription(
                ruleInfo.RichRuleDescriptionDto.introductionHtmlContent,
                ruleInfo.RichRuleDescriptionDto.tabs
                    .Select<RuleDescriptionTabDto, IRuleDescriptionTab>(
                        tab =>
                        {
                            if (tab.content.Left != null)
                            {
                                return new NonContextualRuleDescriptionTab(tab.title, tab.content.Left.htmlContent);
                            }

                            return new ContextualRuleDescriptionTab(tab.title,
                                tab.content.Right.defaultContextKey,
                                tab.content.Right.contextualSections
                                    .Select(context =>
                                        new ContextualRuleDescriptionTab.ContextContentTab(context.displayName,
                                            context.contextKey,
                                            context.htmlContent))
                                    .ToList());
                        })
                    .ToList());
    }
}
