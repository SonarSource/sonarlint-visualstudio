﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using SonarLint.VisualStudio.Education.Rule;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    internal interface ISimpleRuleHelpXamlBuilder
    {
        /// <summary>
        /// Generates a XAML document containing the help information for the specified rule
        /// </summary>
        /// <remarks>Assumes that the <see cref="IRuleInfo.Description"/> and <see cref="IRuleInfo.DescriptionSections"/> are parseable as XML.
        /// Also assumes that the containing control defines a list of Style resources, one for each
        /// value in the enum <see cref="StyleResourceNames"/>.
        /// The document will still render if a style is missing, but the styling won't be correct.</remarks>
        /// <param name="ruleInfo">Rule description information</param>
        FlowDocument Create(IRuleInfo ruleInfo);
    }

    [Export(typeof(ISimpleRuleHelpXamlBuilder))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SimpleRuleHelpXamlBuilder : ISimpleRuleHelpXamlBuilder
    {
        private readonly IXamlGeneratorHelperFactory xamlGeneratorHelperFactory;
        private readonly IRuleHelpXamlTranslator ruleHelpXamlTranslator;
        private readonly IXamlWriterFactory xamlWriterFactory;

        [ImportingConstructor]
        public SimpleRuleHelpXamlBuilder(IRuleHelpXamlTranslatorFactory ruleHelpXamlTranslatorFactory, IXamlGeneratorHelperFactory xamlGeneratorHelperFactory, IXamlWriterFactory xamlWriterFactory)
        {
            this.xamlGeneratorHelperFactory = xamlGeneratorHelperFactory;
            this.xamlWriterFactory = xamlWriterFactory;
            ruleHelpXamlTranslator = ruleHelpXamlTranslatorFactory.Create();
        }

        public FlowDocument Create(IRuleInfo ruleInfo)
        {
            var xaml = CreateXamlString(ruleInfo);
            var flowDocument = (FlowDocument)XamlReader.Parse(xaml);

            return flowDocument;
        }

        private string CreateXamlString(IRuleInfo ruleInfo)
        {
            var sb = new StringBuilder();
            var writer = xamlWriterFactory.Create(sb);
            var helper = xamlGeneratorHelperFactory.Create(writer);

            helper.WriteDocumentHeader(ruleInfo);
            writer.WriteRaw(ruleHelpXamlTranslator.TranslateHtmlToXaml(ruleInfo.Description));
            helper.EndDocument();

            return sb.ToString();
        }
    }
}
