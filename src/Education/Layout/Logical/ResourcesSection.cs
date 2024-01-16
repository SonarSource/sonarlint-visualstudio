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
using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    internal class ResourcesSection : IRichRuleDescriptionSection
    {
        public const string RuleInfoKey = "resources";
        internal /* for testing */ readonly string partialXamlContent;
        internal /* for testing */ readonly IReadOnlyList<string> educationPrinciples;

        public ResourcesSection(string partialXaml, IReadOnlyList<string> educationPrinciples)
        {
            partialXamlContent = partialXaml;
            this.educationPrinciples = educationPrinciples;
        }

        public string Key => RuleInfoKey;
        public string Title => "More info";

        public IAbstractVisualizationTreeNode GetVisualizationTreeNode(IStaticXamlStorage staticXamlStorage)
        {
            var sections = new List<IAbstractVisualizationTreeNode>
            {
                new ContentSection(staticXamlStorage.ResourcesHeader),
                new ContentSection(partialXamlContent)
            };

            if (educationPrinciples != null && educationPrinciples.Count != 0)
            {
                sections.Add(new ContentSection(staticXamlStorage.EducationPrinciplesHeader));                                                        
                foreach (var educationPrinciple in educationPrinciples)
                {
                    string educationPrincipleXamlContent = null;
                    switch (educationPrinciple)
                    {
                        case "defense_in_depth":
                            educationPrincipleXamlContent = staticXamlStorage.EducationPrinciplesDefenseInDepth;
                            break;
                        case "never_trust_user_input":
                            educationPrincipleXamlContent = staticXamlStorage.EducationPrinciplesNeverTrustUserInput;
                            break;
                    }

                    if (educationPrincipleXamlContent != null)
                    {

                        sections.Add(new BorderedSection(new ContentSection(educationPrincipleXamlContent)));
                    }
                }
            }

            return new MultiBlockSection(sections);
        }
    }
}
