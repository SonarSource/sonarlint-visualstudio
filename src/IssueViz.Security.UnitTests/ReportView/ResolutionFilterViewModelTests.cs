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

using System.ComponentModel;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class ResolutionFilterViewModelTests
{
    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void Ctor_InitializesProperties(bool isResolved, bool isSelected)
    {
        var resolutionFilterViewModel = new ResolutionFilterViewModel(isResolved, isSelected);

        resolutionFilterViewModel.IsResolved.Should().Be(isResolved);
        resolutionFilterViewModel.IsSelected.Should().Be(isSelected);
    }

    [TestMethod]
    public void Ctor_InitializesTitleBasedOnIsResolved_Resolved()
    {
        var resolutionFilterViewModel = new ResolutionFilterViewModel(isResolved: true, isSelected: true);

        resolutionFilterViewModel.Title.Should().Be(Resources.ResolutionFilter_Resolved);
    }

    [TestMethod]
    public void Ctor_InitializesTitleBasedOnIsResolved_Open()
    {
        var resolutionFilterViewModel = new ResolutionFilterViewModel(isResolved: false, isSelected: true);

        resolutionFilterViewModel.Title.Should().Be(Resources.ResolutionFilter_Open);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void IsSelected_Setter_ChangesPropertyAndRaisesPropertyChanged(bool initialValue, bool newValue)
    {
        var viewModel = new ResolutionFilterViewModel(isResolved: true, isSelected: initialValue);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        viewModel.PropertyChanged += eventHandler;

        viewModel.IsSelected = newValue;

        viewModel.IsSelected.Should().Be(newValue);
        eventHandler.Received(1).Invoke(viewModel, Arg.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(viewModel.IsSelected)));
    }
}
