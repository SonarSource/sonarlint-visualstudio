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
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion.DiffView;

[TestClass]
public class DiffViewViewModelTests
{
    private const string FilePath = "C:\\myFile.text";
    private readonly List<ChangesDto> twoChangesDtos = [CreateChangeDto(1, 1, "var a=1;"), CreateChangeDto(2, 2, "var b=0;")];
    private DiffViewViewModel testSubject;
    private ITextBuffer textBuffer;
    private ITextViewEditor textViewEditor;

    [TestInitialize]
    public void TestInitialize()
    {
        textViewEditor = Substitute.For<ITextViewEditor>();
        textBuffer = Substitute.For<ITextBuffer>();
        MockTextBufferGetFilePath(FilePath);

        testSubject = new DiffViewViewModel(textViewEditor, textBuffer, twoChangesDtos);
    }

    [TestMethod]
    public void ViewModel_InheritsViewModelBase() => testSubject.Should().BeAssignableTo<ViewModelBase>();

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        testSubject.ChangeViewModels.Select(vm => vm.ChangeDto).Should().BeEquivalentTo(twoChangesDtos);
        testSubject.TextBuffer.Should().Be(textBuffer);
        testSubject.FilePath.Should().Be(FilePath);
        testSubject.FileName.Should().Be("myFile.text");
        testSubject.Before.Should().BeNull();
        testSubject.After.Should().BeNull();
    }

    [TestMethod]
    public void Ctor_AllChangesAreSelected() => testSubject.ChangeViewModels.Should().OnlyContain(vm => vm.IsSelected);

    [TestMethod]
    public void InitializeBeforeAndAfter_InitializesBeforeAndAfter()
    {
        var beforeBuffer = Substitute.For<ITextBuffer>();
        var afterBuffer = Substitute.For<ITextBuffer>();
        textViewEditor.CreateTextBuffer(Arg.Any<string>(), Arg.Any<IContentType>()).Returns(beforeBuffer, afterBuffer);

        testSubject.InitializeBeforeAndAfter();

        testSubject.Before.Should().Be(beforeBuffer);
        testSubject.After.Should().Be(afterBuffer);
        textViewEditor.Received(2).CreateTextBuffer(textBuffer.CurrentSnapshot.GetText(), textBuffer.ContentType);
    }

    [TestMethod]
    public void InitializeBeforeAndAfter_NoChangeSelected_DoesNotApplyChanges()
    {
        testSubject.ChangeViewModels.ForEach(vm => vm.IsSelected = false);

        testSubject.InitializeBeforeAndAfter();

        textViewEditor.DidNotReceive().ApplyChanges(testSubject.After, Arg.Any<List<ChangesDto>>(), abortOnOriginalTextChanged: false);
    }

    [TestMethod]
    public void InitializeBeforeAndAfter_OneChangeSelected_ApplyChange()
    {
        testSubject.ChangeViewModels[0].IsSelected = false;
        testSubject.ChangeViewModels[1].IsSelected = true;

        testSubject.InitializeBeforeAndAfter();

        textViewEditor.Received(1).ApplyChanges(testSubject.After, Arg.Is<List<ChangesDto>>(dtos => dtos.Count == 1 && dtos[0] == testSubject.ChangeViewModels[1].ChangeDto),
            abortOnOriginalTextChanged: false);
    }

    [TestMethod]
    public void CalculateAfter_NoChangeSelected_RecreatesBufferButDoesNotApplyChanges()
    {
        var afterBuffer = Substitute.For<ITextBuffer>();
        textViewEditor.CreateTextBuffer(Arg.Any<string>(), Arg.Any<IContentType>()).Returns(afterBuffer);
        testSubject.ChangeViewModels.ForEach(vm => vm.IsSelected = false);

        testSubject.CalculateAfter();

        testSubject.After.Should().Be(afterBuffer);
        textViewEditor.DidNotReceive().ApplyChanges(testSubject.After, Arg.Any<List<ChangesDto>>(), abortOnOriginalTextChanged: false);
    }

    [TestMethod]
    public void CalculateAfter_OneChangeSelected_ApplyChange()
    {
        testSubject.ChangeViewModels[0].IsSelected = false;
        testSubject.ChangeViewModels[1].IsSelected = true;

        testSubject.CalculateAfter();

        textViewEditor.Received(1).ApplyChanges(testSubject.After, Arg.Is<List<ChangesDto>>(dtos => dtos.Count == 1 && dtos[0] == testSubject.ChangeViewModels[1].ChangeDto),
            abortOnOriginalTextChanged: false);
    }

    private void MockTextBufferGetFilePath(string path)
    {
        var textDocument = Substitute.For<ITextDocument>();
        textDocument.FilePath.Returns(path);
        var propertyCollection = new PropertyCollection();
        propertyCollection.AddProperty(typeof(ITextDocument), textDocument);
        textBuffer.Properties.Returns(propertyCollection);
    }

    private static ChangesDto CreateChangeDto(int beforeLine, int afterLine, string after) => new(new LineRangeDto(beforeLine, afterLine), string.Empty, after);
}
