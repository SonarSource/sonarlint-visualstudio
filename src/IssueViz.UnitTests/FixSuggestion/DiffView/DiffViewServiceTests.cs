/*
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
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion.DiffView;

[TestClass]
public class DiffViewServiceTests
{
    private readonly FixSuggestionDetails fixSuggestionDetails = new(1, 3, "C://somePath/myFile.cs");
    private IDiffViewToolWindowPane diffViewToolWindowPane;
    private DiffViewService testSubject;
    private ITextBufferFactoryService textBufferFactoryService;
    private IToolWindowService toolWindowService;

    [TestInitialize]
    public void TestInitialize()
    {
        toolWindowService = Substitute.For<IToolWindowService>();
        textBufferFactoryService = Substitute.For<ITextBufferFactoryService>();
        diffViewToolWindowPane = Substitute.For<IDiffViewToolWindowPane>();
        toolWindowService.GetToolWindow<DiffViewToolWindowPane, IDiffViewToolWindowPane>().Returns(diffViewToolWindowPane);

        testSubject = new DiffViewService(
            toolWindowService,
            textBufferFactoryService);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DiffViewService, IDiffViewService>(
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<ITextBufferFactoryService>());

    [TestMethod]
    public void MefCtor_CheckIsNonShared() => MefTestHelpers.CheckIsNonSharedMefComponent<DiffViewService>();

    [TestMethod]
    public void Ctor_InstantiatesDiffViewWindowToolPane() => toolWindowService.Received(1).GetToolWindow<DiffViewToolWindowPane, IDiffViewToolWindowPane>();

    [TestMethod]
    public void ShowDiffView_CallsShowDiffWithCorrectParameters()
    {
        var before = CreateChangeModel("int a=1;");
        var after = CreateChangeModel("var a=1;");
        var expectedBeforeTextBuffer = MockTextBuffer(before);
        var expectedAfterTextBuffer = MockTextBuffer(after);

        testSubject.ShowDiffView(fixSuggestionDetails, before, after);

        diffViewToolWindowPane.Received(1).ShowDiff(fixSuggestionDetails, expectedBeforeTextBuffer, expectedAfterTextBuffer);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ShowDiffView_ReturnsResultFromToolWindowPane(bool expectedResult)
    {
        diffViewToolWindowPane.ShowDiff(fixSuggestionDetails, Arg.Any<ITextBuffer>(), Arg.Any<ITextBuffer>()).Returns(expectedResult);

        var applied = testSubject.ShowDiffView(fixSuggestionDetails, CreateChangeModel(string.Empty), CreateChangeModel(";"));

        applied.Should().Be(expectedResult);
    }

    private ITextBuffer MockTextBuffer(ChangeModel change)
    {
        var textBuffer = Substitute.For<ITextBuffer>();
        textBufferFactoryService.CreateTextBuffer(change.Text, change.ContentType).Returns(textBuffer);
        return textBuffer;
    }

    private static ChangeModel CreateChangeModel(string text) => new(text, Substitute.For<IContentType>());
}
