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
using System.Collections.Generic;
using System.Linq;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    internal class ContextualRuleDescriptionTab : IRuleDescriptionTab
    {
        private readonly List<ContextContentTab> contexts;
        private readonly string defaultContext;

        public ContextualRuleDescriptionTab(string title, string defaultContext, List<ContextContentTab> contexts)
        {
            Title = title;
            this.contexts = contexts;
            this.defaultContext = defaultContext;
        }

        public string Title { get; }

        public IAbstractVisualizationTreeNode ProduceVisualNode(VisualizationParameters parameters)
        {
            var contextTabs = contexts
                .Select(x => new TabItem(x.Title,
                    new ContentSection(parameters.HtmlToXamlTranslator.TranslateHtmlToXaml(x.HtmlContent))))
                .ToList<ITabItem>();

            return new TabGroup(contextTabs, GetSelectedTabIndex(parameters.RelevantContext));
        }

        private int GetSelectedTabIndex(string contextToDisplay)
        {
            var selectedIndex = FindContextIndex(contextToDisplay);

            if (selectedIndex == -1)
            {
                selectedIndex = FindContextIndex(defaultContext);
            }

            return Math.Max(selectedIndex, 0);
        }

        private int FindContextIndex(string context)
        {
            if (context == null)
            {
                return -1;
            }
            
            return contexts.FindIndex(x => x.ContextKey.Equals(context));
        }

        internal class ContextContentTab
        {
            public ContextContentTab(string title, string contextKey, string htmlContent)
            {
                Title = title;
                ContextKey = contextKey;
                HtmlContent = htmlContent;
            }

            public string Title { get; }
            public string ContextKey { get; }
            public string HtmlContent { get; }
        }
    }
}
