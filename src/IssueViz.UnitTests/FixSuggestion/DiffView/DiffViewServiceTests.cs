﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion.DiffView;

[TestClass]
public class DiffViewServiceTests
{
    private readonly List<FixSuggestionChange> twoChanges = [CreateChange(1, 1, "var a=1;"), CreateChange(2, 2, "var b=0;")];
    private IDiffViewToolWindowPane diffViewToolWindowPane;
    private DiffViewService testSubject;
    private ITextBuffer textBuffer;
    private ITextViewEditor textViewEditor;
    private IToolWindowService toolWindowService;

    [TestInitialize]
    public void TestInitialize()
    {
        toolWindowService = Substitute.For<IToolWindowService>();
        textViewEditor = Substitute.For<ITextViewEditor>();
        diffViewToolWindowPane = Substitute.For<IDiffViewToolWindowPane>();
        toolWindowService.GetToolWindow<DiffViewToolWindowPane, IDiffViewToolWindowPane>().Returns(diffViewToolWindowPane);
        textBuffer = Substitute.For<ITextBuffer>();

        testSubject = new DiffViewService(toolWindowService, textViewEditor);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DiffViewService, IDiffViewService>(
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<ITextViewEditor>());

    [TestMethod]
    public void MefCtor_CheckIsNonShared() => MefTestHelpers.CheckIsNonSharedMefComponent<DiffViewService>();

    [TestMethod]
    public void Ctor_InstantiatesDiffViewWindowToolPane() => toolWindowService.Received(1).GetToolWindow<DiffViewToolWindowPane, IDiffViewToolWindowPane>();

    [TestMethod]
    public void ShowDiffView_CallsShowDiffWithCorrectParameters()
    {
        testSubject.ShowDiffView(textBuffer, twoChanges);

        diffViewToolWindowPane.Received(1).ShowDiff(Arg.Is<DiffViewViewModel>(vm => vm.TextBuffer == textBuffer && vm.ChangeViewModels.Select(x => x.Change).SequenceEqual(twoChanges)));
    }

    [TestMethod]
    public void ShowDiffView_ReturnsResultFromToolWindowPane()
    {
        var finalizedChangesToReturn = new FinalizedFixSuggestionChange[10];
        diffViewToolWindowPane.ShowDiff(Arg.Is<DiffViewViewModel>(x => x.ChangeViewModels.Select(y => y.Change).SequenceEqual(twoChanges))).Returns(finalizedChangesToReturn);

        var finalizedChanges = testSubject.ShowDiffView(textBuffer, twoChanges);

        finalizedChanges.Should().BeSameAs(finalizedChangesToReturn);
    }

    private static FixSuggestionChange CreateChange(int startLine, int endLine, string after) => new(startLine, endLine, string.Empty, after);
}
