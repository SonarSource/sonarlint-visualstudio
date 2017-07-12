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

using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix;
using Microsoft.VisualStudio.Utilities;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TaggerProviderTests
    {
        private ISonarLintDaemon daemon;
        private Mock<ISonarLintDaemon> mockDaemon;

        private string filename = "foo.js";
        private Mock<ITextDocument> mockTextDocument;

        private TaggerProvider provider;

        private IContentType jsContentType;

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

            var mockContentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            mockContentTypeRegistryService.Setup(c => c.ContentTypes).Returns(Enumerable.Empty<IContentType>());
            var contentTypeRegistryService = mockContentTypeRegistryService.Object;

            var mockProject = new Mock<Project>();
            mockProject.Setup(p => p.Name).Returns("MyProject");
            var project = mockProject.Object;

            var mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.Setup(s => s.ContainingProject).Returns(project);
            var projectItem = mockProjectItem.Object;

            var mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>())).Returns(projectItem);
            var solution = mockSolution.Object;

            var mockDTE = new Mock<_DTE>();
            mockDTE.Setup(d => d.Solution).Returns(solution);
            var dte = mockDTE.Object;

            var mockSVsServiceProvider = new Mock<SVsServiceProvider>();
            mockSVsServiceProvider.Setup(s => s.GetService(typeof(_DTE))).Returns(dte);
            var SVsServiceProvider = mockSVsServiceProvider.Object;

            var mockFileExtensionRegistryService = new Mock<IFileExtensionRegistryService>();
            var fileExtensionRegistryService = mockFileExtensionRegistryService.Object;

            var mockJsContentType = new Mock<IContentType>();
            mockJsContentType.Setup(c => c.IsOfType(It.IsAny<string>())).Returns(false);
            mockJsContentType.Setup(c => c.IsOfType("JavaScript")).Returns(true);
            this.jsContentType = mockJsContentType.Object;


            this.mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(d => d.FilePath).Returns(filename);
            mockTextDocument.Setup(d => d.Encoding).Returns(Encoding.UTF8);
            var textDocument = mockTextDocument.Object;

            mockTextDocumentFactoryService
                .Setup(t => t.TryGetTextDocument(It.IsAny<ITextBuffer>(), out textDocument))
                .Returns(true);

            this.provider = new TaggerProvider(tableManagerProvider, textDocumentFactoryService, contentTypeRegistryService, fileExtensionRegistryService, daemon, SVsServiceProvider);
        }

        [TestMethod]
        public void CreateTagger_should_create_tracker_for_js_when_daemon_running()
        {
            CreateTagger(jsContentType).Should().NotBeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_when_daemon_not_running()
        {
            mockDaemon.Setup(d => d.IsRunning).Returns(false);

            CreateTagger(jsContentType).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_not_js()
        {
            SetMockDocumentFilename(filename + ".java");

            CreateTagger().Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_already_tracked_file()
        {
            CreateTagger(jsContentType).Should().NotBeNull();
            CreateTagger(jsContentType).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_already_tracked_renamed_file()
        {
            CreateTagger(jsContentType).Should().NotBeNull();

            var newName = "bar-" + filename;
            provider.Rename(filename, newName);
            SetMockDocumentFilename(newName);

            CreateTagger(jsContentType).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_track_again_after_reopen()
        {
            var tracker = CreateTagger(jsContentType) as IssueTracker;

            CreateTagger(jsContentType).Should().BeNull();

            tracker.Dispose();

            CreateTagger(jsContentType).Should().NotBeNull();
        }

        [TestMethod]
        public void CreateTagger_should_track_by_case_insensitive_name()
        {
            var lower = "foo.js";
            SetMockDocumentFilename(lower);
            CreateTagger(jsContentType).Should().NotBeNull();

            SetMockDocumentFilename(lower.ToUpperInvariant());
            CreateTagger(jsContentType).Should().BeNull();

            var upper = "BAR.JS";
            SetMockDocumentFilename(upper);
            CreateTagger(jsContentType).Should().NotBeNull();

            SetMockDocumentFilename(upper.ToLowerInvariant());
            CreateTagger(jsContentType).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_be_distinct_per_file()
        {
            var tagger1 = CreateTagger(jsContentType);

            SetMockDocumentFilename("bar-" + filename);

            var tagger2 = CreateTagger(jsContentType);
            tagger2.Should().NotBeNull();
            tagger1.Should().NotBe(tagger2);
        }

        [TestMethod]
        public void Should_not_crash_on_issues_in_untracked_files()
        {
            var issues = new List<Issue> { new Issue() };
            provider.Accept("nonexistent.js", issues);
        }

        [TestMethod]
        public void Should_propagate_new_tracker_factories_to_existing_sink_managers()
        {
            var mockTableDataSink1 = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink1.Object);

            var mockTableDataSink2 = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink2.Object);

            var tracker = CreateTagger(jsContentType) as IssueTracker;

            // factory of new tracker is propagated to all existing sink managers
            mockTableDataSink1.Verify(s => s.AddFactory(tracker.Factory, false));
            mockTableDataSink2.Verify(s => s.AddFactory(tracker.Factory, false));
        }

        [TestMethod]
        public void Should_not_crash_when_sink_subscriber_gone()
        {
            var mockTableDataSink1 = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink1.Object);

            var mockTableDataSink2 = new Mock<ITableDataSink>(MockBehavior.Strict);
            var sinkManager = provider.Subscribe(mockTableDataSink2.Object);

            sinkManager.Dispose();

            var tracker = CreateTagger(jsContentType) as IssueTracker;

            // factory of new tracker is propagated to all existing sink managers
            mockTableDataSink1.Verify(s => s.AddFactory(tracker.Factory, false));
        }

        [TestMethod]
        public void Should_propagate_existing_tracker_factories_to_new_sink_managers()
        {
            SetMockDocumentFilename("foo.js");
            var tracker1 = CreateTagger(jsContentType) as IssueTracker;

            SetMockDocumentFilename("bar.js");
            var tracker2 = CreateTagger(jsContentType) as IssueTracker;

            var mockTableDataSink = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink.Object);

            // factories of existing trackers are propagated to new sink manager
            mockTableDataSink.Verify(s => s.AddFactory(tracker1.Factory, false));
            mockTableDataSink.Verify(s => s.AddFactory(tracker2.Factory, false));
        }

        private ITagger<IErrorTag> CreateTagger(IContentType bufferContentType = null)
        {
            var mockTextBuffer = new Mock<ITextBuffer>();
            mockTextBuffer.Setup(b => b.ContentType).Returns(bufferContentType);
            ITextBuffer textBuffer = mockTextBuffer.Object;

            var mockTextDataModel = new Mock<ITextDataModel>();
            var textDataModel = mockTextDataModel.Object;

            var mockTextView = new Mock<ITextView>();
            mockTextView.Setup(t => t.TextBuffer).Returns(textBuffer);
            mockTextView.Setup(t => t.TextDataModel).Returns(textDataModel);
            ITextView textView = mockTextView.Object;

            return provider.CreateTagger<IErrorTag>(textView, textBuffer);
        }

        private void SetMockDocumentFilename(string filename)
        {
            mockTextDocument.Setup(d => d.FilePath).Returns(filename);
        }
    }
}
