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

using SonarLint.VisualStudio.Education.Layout.Visual;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.Layout.Logical
{
    internal class RootCauseSection : IRichRuleDescriptionSection
    {
        public const string RuleInfoKey = "root_cause";
        internal /* for testing */ readonly string partialXamlContent;

        public RootCauseSection(string partialXaml, bool isHotspot)
        {
            partialXamlContent = partialXaml;
            Title = isHotspot ? "What's the risk?" : "Why is this an issue?";
        }

        public string Key => RuleInfoKey;

        public string Title { get; }

        public IAbstractVisualizationTreeNode GetVisualizationTreeNode(IStaticXamlStorage staticXamlStorage)
        {
            return new ContentSection(partialXamlContent);
        }
    }
}
