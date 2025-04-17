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
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix.Settings.FileExclusions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings.FileExclusions;

[TestClass]
public class FileExclusionsViewModelTests
{
    private const string Pattern1 = "**/*.css";
    private const string Pattern2 = "MyFolder/MyFile.cs";
    private static readonly ExclusionViewModel CssExclusionViewModel = new(Pattern1);
    private IBrowserService browserService;
    private FileExclusionsViewModel testSubject;
    private IUserSettingsProvider userSettingsProvider;

    [TestInitialize]
    public void Initialize()
    {
        browserService = Substitute.For<IBrowserService>();
        userSettingsProvider = Substitute.For<IUserSettingsProvider>();
        MockUserSettingsProvider();

        testSubject = new FileExclusionsViewModel(browserService, userSettingsProvider);
    }

    [TestMethod]
    public void Ctor_ExclusionsExistInUserSettingsFile_InitializesExclusions()
    {
        MockUserSettingsProvider(Pattern1, Pattern2);

        testSubject = new FileExclusionsViewModel(browserService, userSettingsProvider);

        testSubject.Exclusions.Should().HaveCount(2);
        testSubject.Exclusions[0].Pattern.Should().Contain(Pattern1);
        testSubject.Exclusions[1].Pattern.Should().Contain(Pattern2);
    }

    [TestMethod]
    public void Ctor_ExclusionsExistInUserSettingsFile_InitializesSelectedExclusion()
    {
        MockUserSettingsProvider(Pattern1, Pattern2);

        testSubject = new FileExclusionsViewModel(browserService, userSettingsProvider);

        testSubject.SelectedExclusion.Should().Be(testSubject.Exclusions[0]);
    }

    [TestMethod]
    public void SelectedExclusion_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedExclusion = CssExclusionViewModel;

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedExclusion)));
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.CanExecuteDelete)));
    }

    [TestMethod]
    public void CanExecuteDelete_SelectedExclusionNotNull_ReturnsTrue()
    {
        testSubject.SelectedExclusion = CssExclusionViewModel;

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
        testSubject.Exclusions.Add(CssExclusionViewModel);
        testSubject.SelectedExclusion = CssExclusionViewModel;

        testSubject.RemoveExclusion();

        testSubject.Exclusions.Should().BeEmpty();
        testSubject.SelectedExclusion.Should().BeNull();
    }

    [TestMethod]
    public void RemoveExclusion_SelectedExclusionNull_DoesNotRemoveExclusion()
    {
        testSubject.Exclusions.Add(CssExclusionViewModel);
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

    [TestMethod]
    public void InitializeExclusions_InitializesExclusionsAndSelectedExclusionProperty()
    {
        MockUserSettingsProvider(Pattern1, Pattern2);

        testSubject.InitializeExclusions();

        testSubject.Exclusions.Should().HaveCount(2);
        testSubject.Exclusions[0].Pattern.Should().Contain(Pattern1);
        testSubject.Exclusions[1].Pattern.Should().Contain(Pattern2);
        testSubject.SelectedExclusion.Should().Be(testSubject.Exclusions[0]);
    }

    [TestMethod]
    public void InitializeExclusions_InvokedMultipleTimes_DoesNotOverrideExclusionsCollection()
    {
        var initialInstance = testSubject.Exclusions;
        userSettingsProvider.UserSettings.Returns(
            new UserSettings(new AnalysisSettings { UserDefinedFileExclusions = [Pattern1] }),
            new UserSettings(new AnalysisSettings { UserDefinedFileExclusions = [Pattern2] })
        );

        testSubject.InitializeExclusions();
        testSubject.InitializeExclusions();

        testSubject.Exclusions.Should().BeSameAs(initialInstance);
    }

    [TestMethod]
    public void SaveExclusions_SavesExclusions()
    {
        testSubject.Exclusions.Add(new ExclusionViewModel(Pattern1));
        testSubject.Exclusions.Add(new ExclusionViewModel(Pattern2));

        testSubject.SaveExclusions();

        userSettingsProvider.Received(1).UpdateFileExclusions(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new List<string> { Pattern1, Pattern2 })));
    }

    [TestMethod]
    public void SaveExclusions_InvalidPatterns_RemovesInvalidPatternsBeforeSave()
    {
        testSubject.Exclusions.Add(new ExclusionViewModel(Pattern1));
        testSubject.Exclusions.Add(new ExclusionViewModel(string.Empty));
        testSubject.Exclusions.Add(new ExclusionViewModel(Pattern2));
        testSubject.Exclusions.Add(new ExclusionViewModel(null));

        testSubject.SaveExclusions();

        userSettingsProvider.Received(1).UpdateFileExclusions(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new List<string> { Pattern1, Pattern2 })));
    }

    private void MockUserSettingsProvider(params string[] exclusions) =>
        userSettingsProvider.UserSettings.Returns(new UserSettings(new AnalysisSettings { UserDefinedFileExclusions = exclusions.ToList() }));
}
