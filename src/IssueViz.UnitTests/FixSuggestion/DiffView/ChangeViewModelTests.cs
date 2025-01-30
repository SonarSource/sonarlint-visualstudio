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

using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion.DiffView;

[TestClass]
public class ChangeViewModelTests
{
    private readonly FixSuggestionChange change = CreateChange(string.Empty, "var a=1;");
    private ChangeViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new ChangeViewModel(change);

    [TestMethod]
    public void ViewModel_InheritsViewModelBase() => testSubject.Should().BeAssignableTo<ViewModelBase>();

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        testSubject.Change.Should().Be(change);
        testSubject.IsSelected.Should().Be(true);
    }

    [TestMethod]
    [DataRow("var a=1;\nvar b=1;\nvar c=1;\n")]
    [DataRow("var a=1;\r\nvar b=1;\r\nvar c=1;\r\n")]
    [DataRow("var a=1;\tvar b=1;\tvar c=1;\t")]
    public void Ctor_After_InitializesAndRemovesNewLinesAndTabs(string textWithNewLines)
    {
        var changeViewModel = new ChangeViewModel(CreateChange(string.Empty, textWithNewLines));

        changeViewModel.AfterPreview.Should().Be("var a=1;var b=1;var c=1;");
    }

    [TestMethod]
    [DataRow("var a=1;\nvar b=1;\nvar c=1;\n")]
    [DataRow("var a=1;\r\nvar b=1;\r\nvar c=1;\r\n")]
    [DataRow("var a=1;\tvar b=1;\tvar c=1;\t")]
    public void Ctor_Before_InitializesAndRemovesNewLinesAndTabs(string textWithNewLines)
    {
        var changeViewModel = new ChangeViewModel(CreateChange(textWithNewLines, string.Empty));

        changeViewModel.BeforePreview.Should().Be("var a=1;var b=1;var c=1;");
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void IsSelected_ModifiesValueOfUnderlyingModel(bool isSelected)
    {
        testSubject.IsSelected = isSelected;

        testSubject.IsSelected.Should().Be(isSelected);
    }

    [TestMethod]
    public void IsSelected_SetValue_RaisesPropertyChanged()
    {
        var eventRaised = false;
        testSubject.PropertyChanged += (sender, args) => eventRaised = true;

        testSubject.IsSelected = true;

        eventRaised.Should().BeTrue();
    }

    [DataRow(true, true, true)]
    [DataRow(false, true, false)]
    [DataRow(true, false, false)]
    [DataRow(false, false, false)]
    [DataTestMethod]
    public void Finalize_ReturnsExpected(bool isSelected, bool dialogResult, bool expectedFinalized)
    {
        testSubject.IsSelected = isSelected;

        var finalizedFixSuggestionChange = testSubject.Finalize(dialogResult);

        finalizedFixSuggestionChange.Change.Should().BeSameAs(testSubject.Change);
        finalizedFixSuggestionChange.IsAccepted.Should().Be(expectedFinalized);
    }

    private static FixSuggestionChange CreateChange(string before, string after) => new(1, 2, before, after);
}
