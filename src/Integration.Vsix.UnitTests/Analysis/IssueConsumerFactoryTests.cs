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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class IssueConsumerFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueConsumerFactory, IIssueConsumerFactory>(
                MefTestHelpers.CreateExport<ISuppressedIssueMatcher>(),
                MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<ILocalHotspotsStoreUpdater>());
        }

        [TestMethod]
        public void Create_InitializedIssueConsumerReturned()
        {
            IIssuesSnapshot publishedIssuesSnapshot = null;
            var validTextDocument = CreateValidTextDocument("updatedfile.txt");
            var updatedTextSnapshot = validTextDocument.TextBuffer.CurrentSnapshot;

            var testSubject = new IssueConsumerFactory(Substitute.For<ISuppressedIssueMatcher>(),
                Substitute.For<IAnalysisIssueVisualizationConverter>(), Substitute.For<ILocalHotspotsStoreUpdater>());

            var consumer = testSubject.Create(validTextDocument,
                "analysisfile.txt",
                Substitute.For<ITextSnapshot>(),
                "project name",
                projectGuid: Guid.NewGuid(),
                x => { publishedIssuesSnapshot = x; });
            consumer.Should().NotBeNull();
            /* The empty issues list is passed as an argument here because
            it's impossible to verify the actual pipeline due to the fact
            that mocking ITextSnapshot in a way that then can be used by a SnapshotSpan takes a lot of effort */
            consumer.Accept("analysisfile.txt", []);

            publishedIssuesSnapshot.Should().NotBeNull();
            publishedIssuesSnapshot.AnalyzedFilePath.Should().Be("updatedfile.txt"); // filename should be updted by this point
            publishedIssuesSnapshot.Issues.Should().BeEquivalentTo([]);
        }

        private static ITextDocument CreateValidTextDocument(string filePath)
        {
            var document = Substitute.For<ITextDocument>();
            var buffer = Substitute.For<ITextBuffer>();
            var snapshot = Substitute.For<ITextSnapshot>();

            document.FilePath.Returns(filePath);
            document.TextBuffer.Returns(buffer);
            buffer.CurrentSnapshot.Returns(snapshot);

            return document;
        }
    }
}
