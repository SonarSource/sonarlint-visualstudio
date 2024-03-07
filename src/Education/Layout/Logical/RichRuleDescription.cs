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

using System.Collections.Generic;
using System.Linq;
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;
using SonarLint.VisualStudio.Education.Rule;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    internal interface IRichRuleDescription : IVisualNodeProducer
    {
    }

    internal class RichRuleDescription : IRichRuleDescription
    {
        internal /* for testing */ readonly string introductionHtml;
        private readonly List<IRuleDescriptionTab> tabs;

        public RichRuleDescription(string introductionHtml, List<IRuleDescriptionTab> tabs)
        {
            this.introductionHtml = HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(introductionHtml);
            this.tabs = tabs;
        }

        public IAbstractVisualizationTreeNode ProduceVisualNode(VisualizationParameters parameters)
        {
            var tabGroup = new TabGroup(tabs.Select(x => new TabItem(x.Title, x.ProduceVisualNode(parameters))).Cast<ITabItem>().ToList(), 0);
            
            if (introductionHtml is null)
            {
                return tabGroup;
            }

            return new MultiBlockSection(
                new ContentSection(parameters.HtmlToXamlTranslator.TranslateHtmlToXaml(introductionHtml)),
                tabGroup);
        }
    }
}
