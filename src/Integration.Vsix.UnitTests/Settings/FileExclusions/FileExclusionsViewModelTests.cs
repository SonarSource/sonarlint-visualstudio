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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Settings.FileExclusions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings.FileExclusions;

[TestClass]
public class FileExclusionsViewModelTests
{
    private static readonly ExclusionViewModel CsExclusionViewModel = new("**/*.cs");
    private IBrowserService browserService;
    private FileExclusionsViewModel testSubject;

    [TestInitialize]
    public void Initialize()
    {
        browserService = Substitute.For<IBrowserService>();
        testSubject = new FileExclusionsViewModel(browserService);
    }

    [TestMethod]
    public void SelectedExclusion_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedExclusion = CsExclusionViewModel;

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedExclusion)));
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.CanExecuteDelete)));
    }

    [TestMethod]
    public void CanExecuteDelete_SelectedExclusionNotNull_ReturnsTrue()
    {
        testSubject.SelectedExclusion = CsExclusionViewModel;

        testSubject.CanExecuteDelete.Should().BeTrue();
    }

    [TestMethod]
    public void CanExecuteDelete_SelectedExclusionNull_ReturnsFalse()
    {
        testSubject.SelectedExclusion = null;

        testSubject.CanExecuteDelete.Should().BeFalse();
    }

    [TestMethod]
    public void AddExclusion_AddsNewExclusion()
    {
        testSubject.AddExclusion();

        testSubject.Exclusions.Should().HaveCount(1);
        testSubject.SelectedExclusion.Should().NotBeNull();
        testSubject.SelectedExclusion.Pattern.Should().Be(string.Empty);
    }

    [TestMethod]
    public void RemoveExclusion_ExclusionNotNull_RemovesExclusion()
    {
        testSubject.Exclusions.Add(CsExclusionViewModel);
        testSubject.SelectedExclusion = CsExclusionViewModel;

        testSubject.RemoveExclusion();

        testSubject.Exclusions.Should().BeEmpty();
        testSubject.SelectedExclusion.Should().BeNull();
    }

    [TestMethod]
    public void RemoveExclusion_SelectedExclusionNull_DoesNotRemoveExclusion()
    {
        testSubject.Exclusions.Add(CsExclusionViewModel);
        testSubject.SelectedExclusion = null;

        testSubject.RemoveExclusion();

        testSubject.Exclusions.Should().HaveCount(1);
        testSubject.SelectedExclusion.Should().BeNull();
    }

    [TestMethod]
    public void ViewInBrowser_CallsBrowserService()
    {
        var uri = "http://localhost:9000";

        testSubject.ViewInBrowser(uri);

        browserService.Received().Navigate(uri);
    }
}
