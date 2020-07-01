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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class IssueMarkerTests
    {
        private readonly SnapshotSpan ValidSnapshotSpan = new SnapshotSpan(Mock.Of<ITextSnapshot>(), 0, 0);
        private const string ValidFilePath = "c:\\data\\file.txt";
        private readonly IAnalysisIssue ValidAnalysisIssue = new DummyAnalysisIssue { StartLine = 123, FilePath = ValidFilePath };

        [TestMethod]
        public void Ctor_PropertiesSetCorrectly()
        {
            var testSubject = new IssueMarker(ValidAnalysisIssue, ValidSnapshotSpan, "text", "hash");

            testSubject.Issue.Should().BeSameAs(ValidAnalysisIssue);
            testSubject.Span.Should().Be(ValidSnapshotSpan);
            testSubject.WholeLineText.Should().Be("text");
            testSubject.LineHash.Should().Be("hash");
        }

        [TestMethod]
        public void Clone_PropertiesCopiedCorrectly()
        {
            var original = new IssueMarker(ValidAnalysisIssue, ValidSnapshotSpan, "text", "hash");
            var testSubject = original.Clone();

            testSubject.Should().NotBeSameAs(original);

            testSubject.Issue.Should().BeSameAs(original.Issue);
            testSubject.Span.Should().Be(original.Span);
            testSubject.WholeLineText.Should().Be(original.WholeLineText);
            testSubject.LineHash.Should().Be(original.LineHash);
        }

        [TestMethod]
        public void IssueMarker_IsFilterable()
        {
            var analysisIssue = new DummyAnalysisIssue
            {
                RuleKey = "my key",
                StartLine = 999,
                FilePath = "x:\\aaa.foo"
            };

            var testSubject = new IssueMarker(analysisIssue, ValidSnapshotSpan, "text", "hash");

            testSubject.Should().BeAssignableTo<IFilterableIssue>();

            var filterable = (IFilterableIssue)testSubject;

            filterable.RuleId.Should().Be(analysisIssue.RuleKey);
            filterable.StartLine.Should().Be(analysisIssue.StartLine);
            filterable.FilePath.Should().Be(analysisIssue.FilePath);
            filterable.ProjectGuid.Should().BeNull();

            filterable.WholeLineText.Should().Be("text");
            filterable.LineHash.Should().Be("hash");
        }
    }
}
