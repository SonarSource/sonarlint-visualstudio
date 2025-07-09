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

using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Analysis;

[TestClass]
public class AnalysisConfigurationProviderListenerTests
{
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private AnalysisConfigurationProviderListener testSubject;
    private TestLogger logger;

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        logger = Substitute.ForPartsOf<TestLogger>();

        testSubject = new AnalysisConfigurationProviderListener(
            activeConfigScopeTracker, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AnalysisConfigurationProviderListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AnalysisConfigurationProviderListener>();

    [TestMethod]
    public void Ctor_InitializesLogContexts() =>
        logger.Received(1).ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.SLCoreAnalysisConfigurationLogContext);

    [TestMethod]
    public void GetBaseDirAsync_NoConfigurationScope_ReturnsNull()
    {
        activeConfigScopeTracker.Current.ReturnsNull();

        var result = testSubject.GetBaseDirAsync(new GetBaseDirParams("any")).Result;

        result.baseDir.Should().BeNull();
    }

    [TestMethod]
    public void GetBaseDirAsync_ConfigurationScopeIdDoesNotMatch_ReturnsNull()
    {
        var configScope = new ConfigurationScope("scope1", CommandsBaseDir: "C:\\workspace\\root");
        activeConfigScopeTracker.Current.Returns(configScope);

        var result = testSubject.GetBaseDirAsync(new GetBaseDirParams("different-scope")).Result;

        result.baseDir.Should().BeNull();
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigurationScopeMismatch, "different-scope", "scope1"));
    }

    [TestMethod]
    public void GetBaseDirAsync_ConfigurationScopeIdMatches_ReturnsCommandsBaseDir()
    {
        const string scopeId = "scope1";
        const string expectedBaseDir = "C:\\workspace\\root";
        var configScope = new ConfigurationScope(scopeId, CommandsBaseDir: expectedBaseDir);
        activeConfigScopeTracker.Current.Returns(configScope);

        var result = testSubject.GetBaseDirAsync(new GetBaseDirParams(scopeId)).Result;

        result.baseDir.Should().Be(expectedBaseDir);
    }

    [DataRow(null, [new string[0]])]
    [DataRow("", [new string[0]])]
    [DataRow("configScopeId", [new[] {@"C:\file1"}])]
    [DataRow("configScopeId123", [new[] {@"C:\file1", @"D:\file"}])]
    [DataTestMethod]
    public void GetInferredAnalysisProperties_AnyValue_ReturnsEmptySet(string configScopeId, string[] files)
    {
        var result = testSubject.GetInferredAnalysisPropertiesAsync(new GetInferredAnalysisPropertiesParams(configScopeId,
                files.Select(x => new FileUri(x)).ToList()))
            .Result;

        result.Should().BeEquivalentTo(new GetInferredAnalysisPropertiesResponse([]),config:options => options.ComparingByMembers<GetInferredAnalysisPropertiesResponse>());
    }
}
