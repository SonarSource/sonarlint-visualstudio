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
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion.DiffView;

[TestClass]
public class ChangeViewModelTests
{
    private readonly ChangesDto changeDto = new(new LineRangeDto(1, 2), string.Empty, "var a=1;");
    private ChangeViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new ChangeViewModel(changeDto, false);

    [TestMethod]
    public void ViewModel_InheritsViewModelBase() => testSubject.Should().BeAssignableTo<ViewModelBase>();

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        testSubject.ChangeDto.Should().Be(changeDto);
        testSubject.IsSelected.Should().BeFalse();
    }

    [TestMethod]
    public void IsSelected_SetValue_RaisesPropertyChanged()
    {
        var eventRaised = false;
        testSubject.PropertyChanged += (sender, args) => eventRaised = true;

        testSubject.IsSelected = true;

        eventRaised.Should().BeTrue();
    }

    [TestMethod]
    public void Line_PointToStartLineOfBeforeChange() => testSubject.Line.Should().Be(changeDto.beforeLineRange.startLine.ToString());
}
