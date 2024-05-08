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

using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.TaintList
{
    [TestClass]
    public class TaintIssuesControlViewModelTestsCaption
    {
        private readonly string DefaultCaption = Resources.TaintToolWindowCaption;

        [TestMethod]
        public void Ctor_NoUnderlyingIssues_DefaultCaptionSet()
        {
            var testCase = new TestCase()
                .ChangeStoreContents(/* none */)
                .SetActiveDocument("any.txt");

            var testSubject = testCase.CreateTestSubject();

            testSubject.WindowCaption.Should().Be(DefaultCaption);
        }

        [TestMethod]
        public void Ctor_HasUnderlyingIssues_NoActiveDoc_DefaultCaptionSet()
        {
            var testCase = new TestCase()
                .ChangeStoreContents("file1.txt", "file2.txt")
                .SetActiveDocument(null);

            var testSubject = testCase.CreateTestSubject();

            testSubject.WindowCaption.Should().Be(DefaultCaption);
        }

        [TestMethod]
        public void Ctor_HasUnderlyingIssues_ActiveDocWithNoIssues_ExpectedCaptionSet()
        {
            var testCase = new TestCase()
                .ChangeStoreContents("file1.txt", "file2.txt")
                .SetActiveDocument("anotherFile.txt");

            var testSubject = testCase.CreateTestSubject();

            testSubject.WindowCaption.Should().Be(DefaultCaption + " (0)");
        }

        [TestMethod]
        public void Ctor_HasUnderlyingIssues_ActiveDocWithIssues_ExpectedCaptionSet()
        {
            var testCase = new TestCase()
                .ChangeStoreContents("active.txt", "anotherFile.txt", "active.txt")
                .SetActiveDocument("active.txt");

            var testSubject = testCase.CreateTestSubject();

            testSubject.WindowCaption.Should().Be(DefaultCaption + " (2)");
        }

        [TestMethod]
        public void ActiveDocChanged_ExpectedCaptionSet()
        {
            var testCase = new TestCase()
                .ChangeStoreContents("file1.txt", "file2.txt", "file2.txt");

            var testSubject = testCase.CreateTestSubject();

            // 1. File with one issue
            testCase.ChangeActiveDocument("file1.txt");
            testSubject.WindowCaption.Should().Be(DefaultCaption + " (1)");

            // 2. File with two issues
            testCase.ChangeActiveDocument("file2.txt");
            testSubject.WindowCaption.Should().Be(DefaultCaption + " (2)");

            // 3. File with no issues
            testCase.ChangeActiveDocument("fileWithNoIssues.txt");
            testSubject.WindowCaption.Should().Be(DefaultCaption + " (0)");

            // 4. No active file
            testCase.ChangeActiveDocument(null);
            testSubject.WindowCaption.Should().Be(DefaultCaption);
        }

        [TestMethod]
        public void StoreChanges_ExpectedCaptionSet()
        {
            var testCase = new TestCase().
                SetActiveDocument("activeDoc.txt");
            var testSubject = testCase.CreateTestSubject();

            // 1. One issue in active doc
            testCase.ChangeStoreContents("activeDoc.txt");
            testSubject.WindowCaption.Should().Be(DefaultCaption + " (1)");

            // 2. Two issues in active doc,
            testCase.ChangeStoreContents("file1.txt", "activeDoc.txt", "activeDoc.txt", "file2.txt");
            testSubject.WindowCaption.Should().Be(DefaultCaption + " (2)");

            // 3. No issues in active doc
            testCase.ChangeStoreContents("file1.txt", "file2.txt");
            testSubject.WindowCaption.Should().Be(DefaultCaption + " (0)");
        }

        [TestMethod]
        public void PropertyChange_EventRaisedIfCaptionChanges()
        {
            var testCase = new TestCase()
                .SetActiveDocument(null)
                .ChangeStoreContents(
                    "file1.txt", // file with one issue
                    "file2.txt", // another file one issue
                    "file3.txt", "file3.txt"); // a file with two issues

            var testSubject = testCase.CreateTestSubject();

            int eventCount = 0;
            PropertyChangedEventArgs suppliedArgs = null;
            testSubject.PropertyChanged += (sender, args) => { eventCount++; suppliedArgs = args; };
            var originalCaption = testSubject.WindowCaption;

            // 1. Change from no active doc with one item -> event
            testCase.ChangeActiveDocument("file1.txt");
            eventCount.Should().Be(1);
            suppliedArgs.PropertyName.Should().Be(nameof(ITaintIssuesControlViewModel.WindowCaption));
            testSubject.WindowCaption.Should().NotBe(originalCaption);

            // 2. Change to another doc with one item -> same caption -> no event
            var modifiedCaption1 = testSubject.WindowCaption;
            testCase.ChangeActiveDocument("file2.txt");

            eventCount.Should().Be(1);
            testSubject.WindowCaption.Should().Be(modifiedCaption1);

            // 3. Change to another doc with two items -> caption changed -> event raised
            testCase.ChangeActiveDocument("file3.txt");
            eventCount.Should().Be(2);
            testSubject.WindowCaption.Should().NotBe(modifiedCaption1);
        }

        private class TestCase
        {
            private readonly Mock<ITaintStore> store = new();
            private readonly Mock<IActiveDocumentTracker> activeDocTracker = new();
            private readonly Mock<IActiveDocumentLocator> activeDocLocator = new();

            public TestCase()
            {
                ChangeStoreContents(/* empty */);
            }

            public TestCase SetActiveDocument(string filePath)
            {
                var textDoc = CreateTextDoc(filePath);
                activeDocLocator.Setup(x => x.FindActiveDocument()).Returns(textDoc);
                return this;
            }

            public TaintIssuesControlViewModel CreateTestSubject()
            {
                return new TaintIssuesControlViewModel(
                    store.Object,
                    Mock.Of<ILocationNavigator>(),
                    activeDocTracker.Object,
                    activeDocLocator.Object,
                    Mock.Of<IShowInBrowserService>(),
                    Mock.Of<ITelemetryManager>(),
                    Mock.Of<IIssueSelectionService>(),
                    Mock.Of<ICommand>(),
                    Mock.Of<IMenuCommandService>(),
                    Mock.Of<ISonarQubeService>(),
                    Mock.Of<INavigateToRuleDescriptionCommand>(),
                    new NoOpThreadHandler());
            }

            public void ChangeActiveDocument(string filePath)
            {
                var textDoc = CreateTextDoc(filePath);
                activeDocTracker.Raise(x => x.ActiveDocumentChanged += null, new ActiveDocumentChangedEventArgs(textDoc));
            }

            /// <summary>
            /// Sets the store contents and raises am event notifying that the contents have changed
            /// </summary>
            /// </remarks>
            public TestCase ChangeStoreContents(params string[] issueVizFilePaths)
            {
                SetStoreContents(issueVizFilePaths);

                // Note: we don't expect the control to care about the specific issues that
                // have been added or removed, so we're returning empty lists for simplicity.
                store.Raise(x => x.IssuesChanged += null, null, new IssuesChangedEventArgs(
                    removedIssues: Array.Empty<IAnalysisIssueVisualization>(),
                    addedIssues: Array.Empty<IAnalysisIssueVisualization>()));

                return this;
            }

            private void SetStoreContents(params string[] issueVizFilePaths)
            {
                var issueVizs = issueVizFilePaths.Select(x => CreateIssueViz(x)).ToArray();
                store.Setup(x => x.GetAll()).Returns(issueVizs);
            }

            private static ITextDocument CreateTextDoc(string filePath)
            {
                if (filePath == null)
                {
                    return null;
                }

                var textDoc = new Mock<ITextDocument>();
                textDoc.Setup(x => x.FilePath).Returns(filePath);
                return textDoc.Object;
            }

            private static IAnalysisIssueVisualization CreateIssueViz(string filePath = "test.cpp")
            {
                var issue = new Mock<ITaintIssue>();

                var issueViz = new Mock<IAnalysisIssueVisualization>();
                issueViz.Setup(x => x.CurrentFilePath).Returns(filePath);
                issueViz.Setup(x => x.Issue).Returns(issue.Object);
                issueViz.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());

                return issueViz.Object;
            }
        }
    }
}
