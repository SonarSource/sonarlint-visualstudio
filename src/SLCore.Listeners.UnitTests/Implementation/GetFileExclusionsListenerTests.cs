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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class GetFileExclusionsListenerTests
{
    private const string ConfigurationScopeId = "testScopeId";
    private static readonly GetFileExclusionsParams GetFileExclusionsParams = new(ConfigurationScopeId);

    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ILogger logger;
    private GetFileExclusionsListener testSubject;
    private IUserSettingsProvider userSettingsProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        userSettingsProvider = Substitute.For<IUserSettingsProvider>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);

        testSubject = new GetFileExclusionsListener(logger, userSettingsProvider, activeConfigScopeTracker);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<GetFileExclusionsListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ListFilesListener>();

    [TestMethod]
    public void Logger_SetsLogContext() => logger.Received(1).ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.FileExclusionsLogContext);

    [TestMethod]
    public async Task GetFileExclusionsAsync_DifferentConfigScope_ReturnsEmptyAndLogs()
    {
        var activeConfigScope = "otherScopeId";
        MockCurrentConfigScope(activeConfigScope);

        var response = await testSubject.GetFileExclusionsAsync(GetFileExclusionsParams);

        response.fileExclusionPatterns.Should().BeEquivalentTo();
        logger.Received(1).WriteLine(SLCoreStrings.ConfigurationScopeMismatch, GetFileExclusionsParams.configurationScopeId, activeConfigScope);
    }

    [TestMethod]
    public async Task GetFileExclusionsAsync_ActiveConfigScopeIsNull_ReturnsEmptyAndLogs()
    {
        MockCurrentConfigScope(null);

        var response = await testSubject.GetFileExclusionsAsync(GetFileExclusionsParams);

        response.fileExclusionPatterns.Should().BeEquivalentTo();
        logger.Received(1).WriteLine(SLCoreStrings.ConfigurationScopeMismatch, GetFileExclusionsParams.configurationScopeId, null);
    }

    [TestMethod]
    public async Task GetFileExclusionsAsync_CorrectConfigScope_FileExclusionsDefined_ReturnsFileExclusionsFromSettings()
    {
        string[] fileExclusions = ["org/sonar/*", "**\\*.css"];
        MockCurrentConfigScope(GetFileExclusionsParams.configurationScopeId);
        MockUserSettingsFileExclusions(fileExclusions);

        var response = await testSubject.GetFileExclusionsAsync(GetFileExclusionsParams);

        response.fileExclusionPatterns.Should().BeEquivalentTo("**/org/sonar/*", "**/*.css");
    }

    [TestMethod]
    public async Task GetFileExclusionsAsync_CorrectConfigScope_SamePatternDefinedMultipleTimes_DoesNotReturnDuplicates()
    {
        MockCurrentConfigScope(GetFileExclusionsParams.configurationScopeId);
        MockUserSettingsFileExclusions("**/*.css", "org/sonar/*", "**/*.css", "**/*.css", "org/sonar/*");

        var response = await testSubject.GetFileExclusionsAsync(GetFileExclusionsParams);

        response.fileExclusionPatterns.Should().BeEquivalentTo("**/*.css", "**/org/sonar/*");
    }

    [TestMethod]
    public async Task GetFileExclusionsAsync_CorrectConfigScope_NoFileExclusionsDefined_ReturnsEmpty()
    {
        MockCurrentConfigScope(GetFileExclusionsParams.configurationScopeId);
        MockUserSettingsFileExclusions(fileExclusions: []);

        var response = await testSubject.GetFileExclusionsAsync(GetFileExclusionsParams);

        response.fileExclusionPatterns.Should().BeEmpty();
    }

    private void MockUserSettingsFileExclusions(params string[] fileExclusions) =>
        userSettingsProvider.UserSettings.Returns(new UserSettings(new AnalysisSettings { UserDefinedFileExclusions = fileExclusions.ToList() }));

    private void MockCurrentConfigScope(string id) => activeConfigScopeTracker.Current.Returns(id != null ? new ConfigurationScope(id) : null);
}
