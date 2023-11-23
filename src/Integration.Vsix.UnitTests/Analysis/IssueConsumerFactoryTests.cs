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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.ConnectedMode.Synchronization;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class IssueConsumerFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueConsumerFactory, IIssueConsumerFactory>(
                MefTestHelpers.CreateExport<IIssueMatcher>(),
                MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<ILocalHotspotsStoreUpdater>());
        }

        [TestMethod]
        public void Create_InitializedIssueConsumerReturned()
        {
            var testSubject = new IssueConsumerFactory(Mock.Of<IIssueMatcher>(), Mock.Of<IAnalysisIssueVisualizationConverter>(), Mock.Of<ILocalHotspotsStoreUpdater>());

            IIssuesSnapshot publishedSnaphot = null;
            var consumer = testSubject.Create(CreateValidTextDocument("file.txt"), "project name", Guid.NewGuid(), x => { publishedSnaphot = x; });

            consumer.Should().NotBeNull();

            consumer.Accept("file.txt", Array.Empty<IAnalysisIssue>());
            publishedSnaphot.Should().NotBeNull();
        }

        private static ITextDocument CreateValidTextDocument(string filePath)
        {
            var document = new Mock<ITextDocument>();
            var buffer = new Mock<ITextBuffer>();
            var snapshot = new Mock<ITextSnapshot>();

            document.Setup(x => x.FilePath).Returns(filePath);
            document.Setup(x => x.TextBuffer).Returns(buffer.Object);
            buffer.Setup(x => x.CurrentSnapshot).Returns(snapshot.Object);

            return document.Object;
        }
    }
}
