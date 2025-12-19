/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class FileAwareTaintStoreTests
{
    private ITaintStore taintStore;
    private IDocumentTracker documentTracker;
    private FileAwareTaintStore testSubject;
    private List<Document> openDocuments;
    private List<IAnalysisIssueVisualization> allIssues;
    private string openFilePath1;
    private string openFilePath2;
    private string closedFilePath;

    [TestInitialize]
    public void TestInitialize()
    {
        taintStore = Substitute.For<ITaintStore>();
        documentTracker = Substitute.For<IDocumentTracker>();
        testSubject = new FileAwareTaintStore(taintStore, documentTracker);
        openFilePath1 = "C:/file1.cs";
        openFilePath2 = "C:/file2.cs";
        closedFilePath = "C:/file3.cs";
        openDocuments =
        [
            new Document(openFilePath1, []),
            new Document(openFilePath2, [])
        ];
        documentTracker.GetOpenDocuments().Returns(openDocuments.ToArray());
        allIssues =
        [
            CreateMockIssue(openFilePath1),
            CreateMockIssue(openFilePath2),
            CreateMockIssue(closedFilePath)
        ];
        taintStore.GetAll().Returns(allIssues);
    }

    [TestMethod]
    public void GetAll_ReturnsIssuesForOpenFilesOnly()
    {
        var result = testSubject.GetAll();
        result.Should().OnlyContain(x => openDocuments.Select(d => d.FullPath).Contains(x.CurrentFilePath));
        result.Should().NotContain(x => x.CurrentFilePath == closedFilePath);
    }

    [TestMethod]
    public void GetAll_NoOpenFiles_ReturnsEmpty()
    {
        documentTracker.GetOpenDocuments().Returns(Array.Empty<Document>());
        var result = testSubject.GetAll();
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void IssuesChanged_Raised_WhenTaintStoreIssuesChanged()
    {
        var added = new[] { CreateMockIssue(openFilePath1),  CreateMockIssue("different path not raised")  };
        var addedFiltered = new[] { added[0] };
        var removed = new[] { CreateMockIssue(openFilePath2), CreateMockIssue("different path still closed") };
        var eventArgs = new IssuesChangedEventArgs(removed, added);
        var handler = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += handler;

        taintStore.IssuesChanged += Raise.EventWith(taintStore, eventArgs);

        handler.Received(1).Invoke(
            testSubject,
            Arg.Is<IssuesChangedEventArgs>(args =>
                args.AddedIssues.SequenceEqual(addedFiltered)
                && args.RemovedIssues.SequenceEqual(removed)
            )
        );
    }

    [TestMethod]
    public void DocumentOpened_RaisesIssuesChangedForThatFile()
    {
        var filePath = closedFilePath;
        var doc = new Document(filePath, []);
        var analysisIssueVisualizations = new[] { CreateMockIssue(filePath) };
        taintStore.GetAll().Returns(analysisIssueVisualizations);
        documentTracker.GetOpenDocuments().Returns(new[] { doc });
        var handler = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += handler;

        documentTracker.DocumentOpened += Raise.EventWith(documentTracker, new DocumentEventArgs(doc));

        handler.Received(1).Invoke(
            testSubject,
            Arg.Is<IssuesChangedEventArgs>(args =>
                args.AddedIssues.Count == 1 &&
                args.AddedIssues.Single().CurrentFilePath == filePath &&
                args.RemovedIssues.Count == 0
            )
        );
    }

    [TestMethod]
    public void DocumentClosed_RaisesIssuesChangedForThatFile()
    {
        var filePath = openFilePath1;
        var doc = new Document(filePath, []);
        var analysisIssueVisualizations = new List<IAnalysisIssueVisualization> { CreateMockIssue(filePath) };
        taintStore.GetAll().Returns(analysisIssueVisualizations);
        var handler = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += handler;

        documentTracker.DocumentClosed += Raise.EventWith(documentTracker, new DocumentEventArgs(doc));

        handler.Received(1).Invoke(
            testSubject,
            Arg.Is<IssuesChangedEventArgs>(args =>
                args.RemovedIssues.Count == 1 &&
                args.RemovedIssues.Single().CurrentFilePath == filePath &&
                args.AddedIssues.Count == 0
            )
        );
    }

    [TestMethod]
    public void DocumentRenamed_RaisesIssuesChangedForOldFilePath()
    {
        var oldFilePath = closedFilePath;
        var newFilePath = "C:/renamed.cs";
        var doc = new Document(newFilePath, []);
        var analysisIssueVisualizations = new[] { CreateMockIssue(oldFilePath) };
        taintStore.GetAll().Returns(analysisIssueVisualizations);
        var handler = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += handler;

        documentTracker.OpenDocumentRenamed += Raise.EventWith(documentTracker, new DocumentRenamedEventArgs(doc, oldFilePath));

        handler.Received(1).Invoke(
            testSubject,
            Arg.Is<IssuesChangedEventArgs>(args =>
                args.RemovedIssues.Count == 1 &&
                args.RemovedIssues.Single().CurrentFilePath == oldFilePath &&
                args.AddedIssues.Count == 0
            )
        );
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents_AndIsIdempotent()
    {
        testSubject.Dispose();
        testSubject.Dispose();
        taintStore.Received(1).IssuesChanged -= Arg.Any<EventHandler<IssuesChangedEventArgs>>();
        documentTracker.Received(1).DocumentOpened -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received(1).DocumentClosed -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received(1).OpenDocumentRenamed -= Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }

    [TestMethod]
    public void ConfigurationScope_DelegatesToTaintStore()
    {
        taintStore.ConfigurationScope.Returns("scope");
        testSubject.ConfigurationScope.Should().Be("scope");
    }

    [TestMethod]
    public void GetAll_WithNoIssues_ReturnsEmpty()
    {
        taintStore.GetAll().Returns(Array.Empty<IAnalysisIssueVisualization>());
        var result = testSubject.GetAll();
        result.Should().BeEmpty();
    }

    private IAnalysisIssueVisualization CreateMockIssue(string filePath)
    {
        var mock = Substitute.For<IAnalysisIssueVisualization>();
        mock.CurrentFilePath = filePath;
        return mock;
    }
}
