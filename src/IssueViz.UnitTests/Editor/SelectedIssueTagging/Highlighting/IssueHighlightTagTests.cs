/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Windows.Media;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text.Classification;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Highlight;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Highlighting
{
    [TestClass]
    public class IssueHighlightTagTests
    {
        [TestMethod]
        public void Ctor_TagWithCorrectDefinition()
        {
            var lightThemeTag = new IssueHighlightTag(Brushes.Black);
            lightThemeTag.Type.Should().Be(LightIssueHighlightFormatDefinition.FormatName);

            var darkThemeTag = new IssueHighlightTag(Brushes.White);
            darkThemeTag.Type.Should().Be(DarkIssueHighlightFormatDefinition.FormatName);
        }

        [TestMethod]
        public void FormatDefinition_MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<LightIssueHighlightFormatDefinition, EditorFormatDefinition>(null, null);
            MefTestHelpers.CheckTypeCanBeImported<DarkIssueHighlightFormatDefinition, EditorFormatDefinition>(null, null);
        }
    }
}
