/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes;

[TestClass]
public class QuickFixServiceTests
{
    private IDocumentTrackerEx documentTracker = null!;
    private IQuickFixApplicationLogic quickFixApplicationLogic = null!;
    private IQuickFixApplication quickFixApplication = null!;
    private IAnalysisIssueVisualization issueViz = null!;
    private QuickFixService testSubject = null!;

    private const string FilePath = "C:\\test\\file.cs";

    [TestInitialize]
    public void TestInitialize()
    {
        documentTracker = Substitute.For<IDocumentTrackerEx>();
        quickFixApplicationLogic = Substitute.For<IQuickFixApplicationLogic>();
        quickFixApplication = Substitute.For<IQuickFixApplication>();
        issueViz = Substitute.For<IAnalysisIssueVisualization>();

        testSubject = new QuickFixService(documentTracker, quickFixApplicationLogic);
    }

    [TestMethod]
    public void CheckIsSingletonMefComponent() =>
        MefTestHelpers.CheckIsSingletonMefComponent<QuickFixService>();

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<QuickFixService, IQuickFixService>(
            MefTestHelpers.CreateExport<IDocumentTrackerEx>(),
            MefTestHelpers.CreateExport<IQuickFixApplicationLogic>());

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CanBeApplied_SnapshotFound_DelegatesToApplicationLogic(bool innerResult)
    {
        var snapshot = Substitute.For<ITextSnapshot>();
        SetupDocumentTracker(FilePath, snapshot);
        quickFixApplicationLogic.CanBeApplied(quickFixApplication, snapshot).Returns(innerResult);

        testSubject.CanBeApplied(quickFixApplication, FilePath).Should().Be(innerResult);

        quickFixApplicationLogic.Received(1).CanBeApplied(quickFixApplication, snapshot);
    }

    [TestMethod]
    public void CanBeApplied_SnapshotNotFound_ReturnsFalse()
    {
        SetupDocumentTrackerNotFound(FilePath);

        testSubject.CanBeApplied(quickFixApplication, FilePath).Should().BeFalse();

        quickFixApplicationLogic.DidNotReceiveWithAnyArgs().CanBeApplied(default, default);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ApplyAsync_SnapshotFound_DelegatesToApplicationLogic(bool innerResult)
    {
        var snapshot = Substitute.For<ITextSnapshot>();
        SetupDocumentTracker(FilePath, snapshot);
        quickFixApplicationLogic
            .ApplyAsync(quickFixApplication, snapshot, issueViz, Arg.Any<CancellationToken>())
            .Returns(innerResult);

        var result = await testSubject.ApplyAsync(quickFixApplication, FilePath, issueViz);

        result.Should().Be(innerResult);
        await quickFixApplicationLogic.Received(1)
            .ApplyAsync(quickFixApplication, snapshot, issueViz, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ApplyAsync_SnapshotNotFound_ReturnsFalse()
    {
        SetupDocumentTrackerNotFound(FilePath);

        var result = await testSubject.ApplyAsync(quickFixApplication, FilePath, issueViz);

        result.Should().BeFalse();
        await quickFixApplicationLogic.DidNotReceiveWithAnyArgs()
            .ApplyAsync(default, default, default, default);
    }

    private void SetupDocumentTracker(string filePath, ITextSnapshot snapshot)
    {
        documentTracker.TryGetCurrentSnapshot(filePath, out Arg.Any<ITextSnapshot>())
            .Returns(x =>
            {
                x[1] = snapshot;
                return true;
            });
    }

    private void SetupDocumentTrackerNotFound(string filePath)
    {
        documentTracker.TryGetCurrentSnapshot(filePath, out Arg.Any<ITextSnapshot>())
            .Returns(false);
    }
}
