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
using System.Collections.Generic;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    internal class HowToFixItSection : IRichRuleDescriptionSection
    {
        public const string RuleInfoKey = "how_to_fix";
        internal /* for testing */ readonly List<HowToFixItSectionContext> contexts;
        internal /* for testing */ readonly string partialXamlContent;

        public HowToFixItSection(string partialXaml)
        {
            partialXamlContent = partialXaml;
        }

        public HowToFixItSection(List<HowToFixItSectionContext> contexts)
        {
            this.contexts = contexts;
        }

        public string Key => RuleInfoKey;

        public string Title => "How can I fix it?";

        public IAbstractVisualizationTreeNode GetVisualizationTreeNode(ITabsRepository tabsRepository)
        {
            throw new NotImplementedException();
            // todo in the next PR
        }
    }

    internal class HowToFixItSectionContext
    {
        public HowToFixItSectionContext(string key, string title, string partialXaml)
        {
            Key = key;
            Title = title;
            PartialXamlContent = partialXaml;
        }

        public string Key { get; }
        public string Title { get; }
        public string PartialXamlContent { get; }
    }
}
