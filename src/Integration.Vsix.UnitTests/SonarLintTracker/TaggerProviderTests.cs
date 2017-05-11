/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TaggerProviderTests
    {
        private ISonarLintDaemon daemon;
        private Mock<ISonarLintDaemon> mockDaemon;

        private ITextView textView;
        private ITextBuffer textBuffer;

        private string filename = "foo.js";
        private Mock<ITextDocument> mockTextDocument;

        private TaggerProvider provider;

        [TestInitialize]
        public void SetUp()
        {
            // minimal setup to create a tagger

            this.mockDaemon = new Mock<ISonarLintDaemon>();
            mockDaemon.Setup(d => d.IsRunning).Returns(true);
            this.daemon = mockDaemon.Object;

            var mockTableManagerProvider = new Mock<ITableManagerProvider>();
            mockTableManagerProvider.Setup(t => t.GetTableManager(StandardTables.ErrorsTable))
                .Returns(new Mock<ITableManager>().Object);
            var tableManagerProvider = mockTableManagerProvider.Object;

            var mockTextDocumentFactoryService = new Mock<ITextDocumentFactoryService>();
            var textDocumentFactoryService = mockTextDocumentFactoryService.Object;

            var mockTextBuffer = new Mock<ITextBuffer>();
            this.textBuffer = mockTextBuffer.Object;

            var mockTextDataModel = new Mock<ITextDataModel>();
            var textDataModel = mockTextDataModel.Object;

            var mockTextView = new Mock<ITextView>();
            mockTextView.Setup(t => t.TextBuffer).Returns(textBuffer);
            mockTextView.Setup(t => t.TextDataModel).Returns(textDataModel);
            this.textView = mockTextView.Object;

            this.mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(d => d.FilePath).Returns(filename);
            mockTextDocument.Setup(d => d.Encoding).Returns(Encoding.UTF8);
            var textDocument = mockTextDocument.Object;

            mockTextDocumentFactoryService
                .Setup(t => t.TryGetTextDocument(It.IsAny<ITextBuffer>(), out textDocument))
                .Returns(true);

            this.provider = new TaggerProvider(tableManagerProvider, textDocumentFactoryService, daemon);
        }

        [TestMethod]
        public void CreateTagger_should_create_tracker_for_js_when_daemon_running()
        {
            CreateTagger().Should().NotBeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_when_daemon_not_running()
        {
            mockDaemon.Setup(d => d.IsRunning).Returns(false);

            CreateTagger().Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_not_js()
        {
            mockTextDocument.Setup(d => d.FilePath).Returns(filename + ".java");

            CreateTagger().Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_already_tracked_file()
        {
            CreateTagger().Should().NotBeNull();
            CreateTagger().Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_already_tracked_renamed_file()
        {
            CreateTagger().Should().NotBeNull();

            var newName = "bar-" + filename;
            provider.Rename(filename, newName);
            mockTextDocument.Setup(d => d.FilePath).Returns(newName);

            CreateTagger().Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_track_again_after_reopen()
        {
            var tracker = CreateTagger() as IssueTracker;

            CreateTagger().Should().BeNull();

            tracker.Dispose();

            CreateTagger().Should().NotBeNull();
        }

        [TestMethod]
        public void CreateTagger_should_track_by_case_insensitive_name()
        {
            var lower = "foo.js";
            mockTextDocument.Setup(d => d.FilePath).Returns(lower);
            CreateTagger().Should().NotBeNull();

            mockTextDocument.Setup(d => d.FilePath).Returns(lower.ToUpperInvariant());
            CreateTagger().Should().BeNull();

            var upper = "BAR.JS";
            mockTextDocument.Setup(d => d.FilePath).Returns(upper);
            CreateTagger().Should().NotBeNull();

            mockTextDocument.Setup(d => d.FilePath).Returns(upper.ToLowerInvariant());
            CreateTagger().Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_be_distinct_per_file()
        {
            var tagger1 = CreateTagger();

            mockTextDocument.Setup(d => d.FilePath).Returns("bar.js");

            var tagger2 = CreateTagger();
            tagger2.Should().NotBeNull();
            tagger1.Should().NotBe(tagger2);
        }

        private ITagger<IErrorTag> CreateTagger()
        {
            return provider.CreateTagger<IErrorTag>(textView, textBuffer);
        }
    }
}
