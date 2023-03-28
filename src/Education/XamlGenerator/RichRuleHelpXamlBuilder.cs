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

using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Layout.Visual.Tabs;
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    internal class RichRuleHelpXamlBuilder
    {
        private readonly IRuleInfoTranslator ruleInfoTranslator;
        private readonly IXamlGeneratorHelperFactory xamlGeneratorHelperFactory;
        private readonly IStaticXamlStorage staticXamlStorage;
        private readonly IXamlWriterFactory xamlWriterFactory;

        public RichRuleHelpXamlBuilder(IRuleInfoTranslator ruleInfoTranslator, IXamlGeneratorHelperFactory xamlGeneratorHelperFactory, IStaticXamlStorage staticXamlStorage, IXamlWriterFactory xamlWriterFactory)
        {
            this.ruleInfoTranslator = ruleInfoTranslator;
            this.xamlGeneratorHelperFactory = xamlGeneratorHelperFactory;
            this.staticXamlStorage = staticXamlStorage;
            this.xamlWriterFactory = xamlWriterFactory;
        }

        public FlowDocument Create(IRuleInfo ruleInfo)
        {
            var richRuleDescriptionSections = ruleInfoTranslator.GetRuleDescriptionSections(ruleInfo).ToList();
            var mainTabGroup = new TabGroup(richRuleDescriptionSections
                .Select(richRuleDescriptionSection =>
                    new TabItem(richRuleDescriptionSection.Title,
                        richRuleDescriptionSection.GetVisualizationTreeNode(staticXamlStorage)))
                .ToList<ITabItem>());

            var sb = new StringBuilder();
            var writer = xamlWriterFactory.Create(sb);
            var helper = xamlGeneratorHelperFactory.Create(writer);

            helper.WriteDocumentHeader(ruleInfo);
            mainTabGroup.ProduceXaml(writer);
            helper.EndDocument();

            return (FlowDocument)XamlReader.Parse(sb.ToString());
        }
    }
}
