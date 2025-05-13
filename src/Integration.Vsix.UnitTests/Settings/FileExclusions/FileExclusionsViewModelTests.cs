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

using System.Collections.Immutable;
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
    private FileExclusionsViewModel globalFileExclusionsViewModel;
    private IGlobalRawSettingsService globalSettingsUpdater;
    private ISolutionRawSettingsService solutionSettingsUpdater;

    [TestInitialize]
    public void Initialize()
    {
        browserService = Substitute.For<IBrowserService>();
        globalSettingsUpdater = Substitute.For<IGlobalRawSettingsService>();
        solutionSettingsUpdater = Substitute.For<ISolutionRawSettingsService>();
        MockGlobalExclusions([]);

        globalFileExclusionsViewModel = new FileExclusionsViewModel(browserService, globalSettingsUpdater, solutionSettingsUpdater, FileExclusionScope.Global);
    }

    [TestMethod]
    public void SelectedExclusion_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        globalFileExclusionsViewModel.PropertyChanged += eventHandler;

        globalFileExclusionsViewModel.SelectedExclusion = CssExclusionViewModel;

        eventHandler.Received(1).Invoke(globalFileExclusionsViewModel, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(globalFileExclusionsViewModel.SelectedExclusion)));
        eventHandler.Received(1).Invoke(globalFileExclusionsViewModel, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(globalFileExclusionsViewModel.IsAnyExclusionSelected)));
    }

    [TestMethod]
    public void IsAnyExclusionSelected_SelectedExclusionNotNull_ReturnsTrue()
    {
        globalFileExclusionsViewModel.SelectedExclusion = CssExclusionViewModel;

        globalFileExclusionsViewModel.IsAnyExclusionSelected.Should().BeTrue();
    }

    [TestMethod]
    public void IsAnyExclusionSelected_SelectedExclusionNull_ReturnsFalse()
    {
        globalFileExclusionsViewModel.SelectedExclusion = null;

        globalFileExclusionsViewModel.IsAnyExclusionSelected.Should().BeFalse();
    }

    [TestMethod]
    public void AddExclusion_AddsNewExclusion()
    {
        const string patternToAdd = "**/*.js";

        globalFileExclusionsViewModel.AddExclusion(patternToAdd);

        globalFileExclusionsViewModel.Exclusions.Should().HaveCount(1);
        globalFileExclusionsViewModel.SelectedExclusion.Should().NotBeNull();
        globalFileExclusionsViewModel.SelectedExclusion.Pattern.Should().Be(patternToAdd);
    }

    [TestMethod]
    public void RemoveExclusion_ExclusionNotNull_RemovesExclusion()
    {
        globalFileExclusionsViewModel.Exclusions.Add(CssExclusionViewModel);
        globalFileExclusionsViewModel.SelectedExclusion = CssExclusionViewModel;

        globalFileExclusionsViewModel.RemoveExclusion();

        globalFileExclusionsViewModel.Exclusions.Should().BeEmpty();
        globalFileExclusionsViewModel.SelectedExclusion.Should().BeNull();
    }

    [TestMethod]
    public void RemoveExclusion_SelectedExclusionNull_DoesNotRemoveExclusion()
    {
        globalFileExclusionsViewModel.Exclusions.Add(CssExclusionViewModel);
        globalFileExclusionsViewModel.SelectedExclusion = null;

        globalFileExclusionsViewModel.RemoveExclusion();

        globalFileExclusionsViewModel.Exclusions.Should().HaveCount(1);
        globalFileExclusionsViewModel.SelectedExclusion.Should().BeNull();
    }

    [TestMethod]
    public void ViewInBrowser_CallsBrowserService()
    {
        var uri = "http://localhost:9000";

        globalFileExclusionsViewModel.ViewInBrowser(uri);

        browserService.Received().Navigate(uri);
    }

    [TestMethod]
    public void InitializeExclusions_WithGlobalFileExclusions_InitializesExclusionsAndSelectedExclusionProperty()
    {
        MockGlobalExclusions([Pattern1, Pattern2]);

        globalFileExclusionsViewModel.InitializeExclusions();

        globalFileExclusionsViewModel.Exclusions.Should().HaveCount(2);
        globalFileExclusionsViewModel.Exclusions[0].Pattern.Should().Contain(Pattern1);
        globalFileExclusionsViewModel.Exclusions[1].Pattern.Should().Contain(Pattern2);
        globalFileExclusionsViewModel.SelectedExclusion.Should().Be(globalFileExclusionsViewModel.Exclusions[0]);
    }

    [TestMethod]
    public void InitializeExclusions_WithSolutionFileExclusions_InitializesExclusionsAndSelectedExclusionProperty()
    {
        MockSolutionExclusions([Pattern1, Pattern2]);
        var solutionFileExclusionsViewModel = new FileExclusionsViewModel(browserService, globalSettingsUpdater, solutionSettingsUpdater, FileExclusionScope.Solution);

        solutionFileExclusionsViewModel.InitializeExclusions();

        solutionFileExclusionsViewModel.Exclusions.Should().HaveCount(2);
        solutionFileExclusionsViewModel.Exclusions[0].Pattern.Should().Contain(Pattern1);
        solutionFileExclusionsViewModel.Exclusions[1].Pattern.Should().Contain(Pattern2);
        solutionFileExclusionsViewModel.SelectedExclusion.Should().Be(solutionFileExclusionsViewModel.Exclusions[0]);
    }

    [TestMethod]
    public void InitializeExclusions_InvokedMultipleTimes_DoesNotOverrideExclusionsCollection()
    {
        var initialInstance = globalFileExclusionsViewModel.Exclusions;
        globalSettingsUpdater.GlobalAnalysisSettings.Returns(new GlobalAnalysisSettings(
                ImmutableDictionary<string, RuleConfig>.Empty, ImmutableArray.Create(Pattern1)),
            new GlobalAnalysisSettings(ImmutableDictionary<string, RuleConfig>.Empty, ImmutableArray.Create(Pattern2))
        );

        globalFileExclusionsViewModel.InitializeExclusions();
        globalFileExclusionsViewModel.InitializeExclusions();

        globalFileExclusionsViewModel.Exclusions.Should().BeSameAs(initialInstance);
    }

    [TestMethod]
    public void SaveExclusions_WithGlobalFileExclusions_SavesExclusions()
    {
        globalFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern1));
        globalFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern2));

        globalFileExclusionsViewModel.SaveExclusions();

        globalSettingsUpdater.Received(1).UpdateFileExclusions(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new List<string> { Pattern1, Pattern2 })));
    }

    [TestMethod]
    public void SaveExclusions_WithSolutionFileExclusions_SavesExclusions()
    {
        var solutionFileExclusionsViewModel = new FileExclusionsViewModel(browserService, globalSettingsUpdater, solutionSettingsUpdater, FileExclusionScope.Solution);
        solutionFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern1));
        solutionFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern2));

        solutionFileExclusionsViewModel.SaveExclusions();

        solutionSettingsUpdater.Received(1).UpdateFileExclusions(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new List<string> { Pattern1, Pattern2 })));
    }

    [TestMethod]
    public void SaveExclusions_WithGlobalFileExclusions_InvalidPatterns_RemovesInvalidPatternsBeforeSave()
    {
        globalFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern1));
        globalFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(string.Empty));
        globalFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern2));
        globalFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(null));

        globalFileExclusionsViewModel.SaveExclusions();

        globalSettingsUpdater.Received(1).UpdateFileExclusions(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new List<string> { Pattern1, Pattern2 })));
    }

    [TestMethod]
    public void SaveExclusions_WithSolutionFileExclusions_InvalidPatterns_RemovesInvalidPatternsBeforeSave()
    {
        var solutionFileExclusionsViewModel = new FileExclusionsViewModel(browserService, globalSettingsUpdater, solutionSettingsUpdater, FileExclusionScope.Solution);
        solutionFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern1));
        solutionFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(string.Empty));
        solutionFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(Pattern2));
        solutionFileExclusionsViewModel.Exclusions.Add(new ExclusionViewModel(null));

        solutionFileExclusionsViewModel.SaveExclusions();

        solutionSettingsUpdater.Received(1).UpdateFileExclusions(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new List<string> { Pattern1, Pattern2 })));
    }

    private void MockGlobalExclusions(string[] globalFileExclusions) => globalSettingsUpdater.GlobalAnalysisSettings.Returns(new GlobalAnalysisSettings([], globalFileExclusions.ToImmutableArray()));

    private void MockSolutionExclusions(string[] solutionFileExclusions) =>
        solutionSettingsUpdater.SolutionAnalysisSettings.Returns(new SolutionAnalysisSettings([], solutionFileExclusions.ToImmutableArray()));
}
