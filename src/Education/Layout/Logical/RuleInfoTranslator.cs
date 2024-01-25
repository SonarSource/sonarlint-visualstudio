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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Education.XamlGenerator;
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    internal interface IRuleInfoTranslator
    {
        IEnumerable<IRichRuleDescriptionSection> GetRuleDescriptionSections(IRuleInfo ruleInfo, string issueContext);
    }

    [Export(typeof(IRuleInfoTranslator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RuleInfoTranslator : IRuleInfoTranslator
    {
        private readonly IRuleHelpXamlTranslator xamlTranslator;
        private readonly ILogger logger;

        [ImportingConstructor]
        public RuleInfoTranslator(IRuleHelpXamlTranslatorFactory xamlTranslatorFactory, ILogger logger)
        {
            this.logger = logger;
            xamlTranslator = xamlTranslatorFactory.Create();
        }

        public IEnumerable<IRichRuleDescriptionSection> GetRuleDescriptionSections(IRuleInfo ruleInfo, string issueContext)
        {
            var sectionsByKey = ruleInfo.DescriptionSections
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.ToList());

            if (sectionsByKey.TryGetValue(RootCauseSection.RuleInfoKey, out var rootCauseSectionContent))
            {
                AssertExpectedNumberOfDescriptionSections(ruleInfo.FullRuleKey, RootCauseSection.RuleInfoKey, rootCauseSectionContent.Count);
                yield return new RootCauseSection(GeneratePartialXaml(rootCauseSectionContent[0].HtmlContent), ruleInfo.IssueType == RuleIssueType.Hotspot);
            }

            if (sectionsByKey.TryGetValue(AssesTheProblemSection.RuleInfoKey, out var assessTheProblemContent))
            {
                AssertExpectedNumberOfDescriptionSections(ruleInfo.FullRuleKey, AssesTheProblemSection.RuleInfoKey, assessTheProblemContent.Count);
                yield return new AssesTheProblemSection(GeneratePartialXaml(assessTheProblemContent[0].HtmlContent));
            }

            if (sectionsByKey.TryGetValue(HowToFixItSection.RuleInfoKey, out var howToFixItContents))
            {
                if (howToFixItContents.Count == 1 && howToFixItContents[0].Context == null)
                {
                    yield return new HowToFixItSection(GeneratePartialXaml(howToFixItContents[0].HtmlContent));
                }
                else
                {
                    yield return new HowToFixItSection(howToFixItContents
                        .Select(x => new HowToFixItSectionContext(x.Context.Key,
                                x.Context.DisplayName,
                                GeneratePartialXaml(x.HtmlContent)))
                        .ToList(),
                        issueContext);
                }
            }

            if (sectionsByKey.TryGetValue(ResourcesSection.RuleInfoKey, out var resourcesSectionContent))
            {
                AssertExpectedNumberOfDescriptionSections(ruleInfo.FullRuleKey, ResourcesSection.RuleInfoKey, resourcesSectionContent.Count);
                yield return new ResourcesSection(GeneratePartialXaml(resourcesSectionContent[0].HtmlContent), ruleInfo.EducationPrinciples);
            }
        }

        private void AssertExpectedNumberOfDescriptionSections(string ruleKey, string descriptionSection, int sectionsCount, int expectedCount = 1)
        {
            if (sectionsCount == expectedCount)
            {
                return;
            }

            var message = $"[{nameof(RuleInfoTranslator)}] Encountered rule description {ruleKey} with unexpected number of section items for {descriptionSection}: expected {expectedCount}, got {sectionsCount}";
            logger.WriteLine(message);
        }

        private string GeneratePartialXaml(string htmlContent)
        {
            return xamlTranslator.TranslateHtmlToXaml(htmlContent);
        }
    }
}
