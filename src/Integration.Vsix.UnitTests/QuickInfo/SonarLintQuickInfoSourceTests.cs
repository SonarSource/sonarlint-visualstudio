/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.QuickInfo
{
    [TestClass]
    public class SonarLintQuickInfoSourceTests
    {
        private Mock<ISonarLintDaemon> daemonMock;
        private Mock<IIssueConverter> issueConverterMock;
        private List<Issue> issues;
        private Mock<IQuickInfoSession> quickInfoSessionMock;
        private SonarLintQuickInfoSource quickInfoSource;
        private Mock<ITextBuffer> subjectBufferMock;
        private Mock<ITextDocumentFactoryService> textDocumentFactoryMock;

        [TestInitialize]
        public void TestInitialize()
        {
            string path = "some path";

            daemonMock = new Mock<ISonarLintDaemon>(MockBehavior.Strict);
            textDocumentFactoryMock = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict);

            issues = new List<Issue>();

            daemonMock
                .Setup(x => x.GetIssues(path))
                .Returns(issues);

            var provider = new SonarLintQuickInfoSourceProvider
            {
                SonarLintDaemon = daemonMock.Object,
                TextDocumentFactoryService = textDocumentFactoryMock.Object,
            };

            subjectBufferMock = new Mock<ITextBuffer>(MockBehavior.Strict);

            issueConverterMock = new Mock<IIssueConverter>(MockBehavior.Strict);

            quickInfoSource = new SonarLintQuickInfoSource(provider, subjectBufferMock.Object, path,
                issueConverterMock.Object);

            quickInfoSessionMock = new Mock<IQuickInfoSession>(MockBehavior.Strict);
        }

        [TestMethod]
        public void Session_Cannot_Find_Trigger_Point()
        {
            // Arrange
            subjectBufferMock
                .SetupGet(x => x.CurrentSnapshot)
                .Returns(default(ITextSnapshot));

            quickInfoSessionMock
                .Setup(x => x.GetTriggerPoint(subjectBufferMock.Object.CurrentSnapshot))
                .Returns((SnapshotPoint?)null);

            // Act
            var content = new List<object>();
            ITrackingSpan applicableToSpan;
            quickInfoSource.AugmentQuickInfoSession(quickInfoSessionMock.Object, content, out applicableToSpan);

            // Assert
            applicableToSpan.Should().BeNull();
            content.Should().BeEmpty();
        }

        [TestMethod]
        public void SonarLint_Daemon_Does_Not_Return_Issues()
        {
            // Arrange
            const int position = 123;

            var textSnapshotMock = new Mock<ITextSnapshot>();
            textSnapshotMock.SetupGet(x => x.Length).Returns(1000);

            subjectBufferMock
                .SetupGet(x => x.CurrentSnapshot)
                .Returns(textSnapshotMock.Object);

            quickInfoSessionMock
                .Setup(x => x.GetTriggerPoint(subjectBufferMock.Object.CurrentSnapshot))
                .Returns(new SnapshotPoint(textSnapshotMock.Object, position));

            // issues list is empty

            // Act
            var content = new List<object>();
            ITrackingSpan applicableToSpan;
            quickInfoSource.AugmentQuickInfoSession(quickInfoSessionMock.Object, content, out applicableToSpan);

            // Assert
            applicableToSpan.Should().BeNull();
            content.Should().BeEmpty();
        }

        [TestMethod]
        public void SonarLint_Daemon_Returns_Issues()
        {
            // Arrange
            const int position = 123;

            var issue = new Issue { Message = "issue 1" }; // We don't care about other properties
            issues.Add(issue);

            var textSnapshotMock = new Mock<ITextSnapshot>();
            textSnapshotMock
                .SetupGet(x => x.Length)
                .Returns(1000);

            var span = new SnapshotSpan(
                        new SnapshotPoint(textSnapshotMock.Object, 100),
                        new SnapshotPoint(textSnapshotMock.Object, 140));

            issueConverterMock
                .Setup(x => x.ToMarker(issue, textSnapshotMock.Object))
                .Returns(new IssueMarker(issue, span));

            textSnapshotMock
                .Setup(x => x.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive))
                .Returns(new Mock<ITrackingSpan>().Object);

            subjectBufferMock
                .SetupGet(x => x.CurrentSnapshot)
                .Returns(textSnapshotMock.Object);

            quickInfoSessionMock
                .Setup(x => x.GetTriggerPoint(subjectBufferMock.Object.CurrentSnapshot))
                .Returns(new SnapshotPoint(textSnapshotMock.Object, position));

            // Act
            var content = new List<object>();
            ITrackingSpan applicableToSpan;
            quickInfoSource.AugmentQuickInfoSession(quickInfoSessionMock.Object, content, out applicableToSpan);

            // Assert
            applicableToSpan.Should().NotBeNull();
            content.Should().HaveCount(1);
            content[0].Should().Be("issue 1");
        }

        [TestMethod]
        public void Ctor_Argument_Checks()
        {
            Action action = () => new SonarLintQuickInfoSource(null, new Mock<ITextBuffer>().Object, "some path", new Mock<IIssueConverter>().Object);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("provider");

            action = () => new SonarLintQuickInfoSource(new SonarLintQuickInfoSourceProvider(), null, "some path", new Mock<IIssueConverter>().Object);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("subjectBuffer");

            action = () => new SonarLintQuickInfoSource(new SonarLintQuickInfoSourceProvider(), new Mock<ITextBuffer>().Object, null, new Mock<IIssueConverter>().Object);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filePath");

            action = () => new SonarLintQuickInfoSource(new SonarLintQuickInfoSourceProvider(), new Mock<ITextBuffer>().Object, "some path", null);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("issueConverter");
        }
    }
}
