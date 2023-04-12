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

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    internal interface IStaticXamlStorage
    {
        string EducationPrinciplesHeader { get; }
        string EducationPrinciplesDefenseInDepth { get; }
        string EducationPrinciplesNeverTrustUserInput { get; }
        string HowToFixItFallbackContext { get; }
        string HowToFixItHeader { get; }
        string ResourcesHeader { get; }
    }

    [Export(typeof(IStaticXamlStorage))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class StaticXamlStorage : IStaticXamlStorage
    {
        private readonly Lazy<string> educationPrinciplesHeader;
        private readonly Lazy<string> educationPrinciplesDefenseInDepth;
        private readonly Lazy<string> educationPrinciplesNeverTrustUserInput;
        private readonly Lazy<string> howToFixItFallbackContext;
        private readonly Lazy<string> howToFixItHeader;
        private readonly Lazy<string> resourcesHeader;

        [ImportingConstructor]
        public StaticXamlStorage(IRuleHelpXamlTranslatorFactory ruleHelpXamlTranslatorFactory)
        {
            var ruleHelpXamlTranslator = ruleHelpXamlTranslatorFactory.Create();
            educationPrinciplesHeader = new Lazy<string>(() => ruleHelpXamlTranslator.TranslateHtmlToXaml(StaticHtmlSnippets.EducationPrinciplesHeader), LazyThreadSafetyMode.None);
            educationPrinciplesDefenseInDepth = new Lazy<string>(() => ruleHelpXamlTranslator.TranslateHtmlToXaml(StaticHtmlSnippets.EducationPrinciplesDefenseInDepth), LazyThreadSafetyMode.None);
            educationPrinciplesNeverTrustUserInput = new Lazy<string>(() => ruleHelpXamlTranslator.TranslateHtmlToXaml(StaticHtmlSnippets.EducationPrinciplesNeverTrustUserInput), LazyThreadSafetyMode.None);
            howToFixItFallbackContext = new Lazy<string>(() => ruleHelpXamlTranslator.TranslateHtmlToXaml(StaticHtmlSnippets.HowToFixItFallbackContext), LazyThreadSafetyMode.None);
            howToFixItHeader = new Lazy<string>(() => ruleHelpXamlTranslator.TranslateHtmlToXaml(StaticHtmlSnippets.HowToFixItHeader), LazyThreadSafetyMode.None);
            resourcesHeader = new Lazy<string>(() => ruleHelpXamlTranslator.TranslateHtmlToXaml(StaticHtmlSnippets.ResourcesHeader), LazyThreadSafetyMode.None);
        }

        public string EducationPrinciplesHeader => educationPrinciplesHeader.Value;
        public string EducationPrinciplesDefenseInDepth => educationPrinciplesDefenseInDepth.Value;
        public string EducationPrinciplesNeverTrustUserInput => educationPrinciplesNeverTrustUserInput.Value;
        public string HowToFixItFallbackContext => howToFixItFallbackContext.Value;
        public string HowToFixItHeader => howToFixItHeader.Value;
        public string ResourcesHeader => resourcesHeader.Value;
    }
}
