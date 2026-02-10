/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.ConnectedMode.ConfigurationScope;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Analysis.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ConfigurationScope;

[TestClass]
public class ConfigurationScopeDotnetAnalyzerIndicatorTests
{
    private TestLogger testLogger;
    private ConfigurationScopeDotnetAnalyzerIndicator testSubject;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private IRoslynAnalyzerService roslynAnalyzerService;

    [TestInitialize]
    public void TestInitialize()
    {
        testLogger = new TestLogger();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        roslynAnalyzerService = Substitute.For<IRoslynAnalyzerService>();
        slCoreServiceProvider.TryGetTransientService(out IRoslynAnalyzerService _).Returns(call =>
        {
            call[0] = roslynAnalyzerService;
            return true;
        });
        testSubject = new ConfigurationScopeDotnetAnalyzerIndicator(slCoreServiceProvider, testLogger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ConfigurationScopeDotnetAnalyzerIndicator, IConfigurationScopeDotnetAnalyzerIndicator>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ConfigurationScopeDotnetAnalyzerIndicator>();
    }

    [TestMethod]
    public async Task ShouldUseEnterpriseCSharpAnalyzerAsync_NullConfigurationScope_ReturnsFalse()
    {
        var result = await testSubject.ShouldUseEnterpriseCSharpAnalyzerAsync(null);

        result.Should().BeFalse();
        testLogger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ShouldUseEnterpriseCSharpAnalyzerAsync_ServiceProviderUnavailable_ReturnsFalse()
    {
        slCoreServiceProvider.TryGetTransientService(out IRoslynAnalyzerService _).ReturnsForAnyArgs(false);
        var result = await testSubject.ShouldUseEnterpriseCSharpAnalyzerAsync("scope");

        result.Should().BeFalse();
        testLogger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task ShouldUseEnterpriseCSharpAnalyzerAsync_ServiceThrows_ReturnsFalse()
    {
        const string exceptionMessage = "exception message";
        roslynAnalyzerService.ShouldUseEnterpriseCSharpAnalyzerAsync(default).ThrowsAsyncForAnyArgs(new Exception(exceptionMessage));
        var result = await testSubject.ShouldUseEnterpriseCSharpAnalyzerAsync("scope");

        result.Should().BeFalse();
        testLogger.AssertPartialOutputStringExists(exceptionMessage);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ShouldUseEnterpriseCSharpAnalyzerAsync_ServiceReturns_ReturnsSameValue(bool toReturn)
    {
        const string configurationScopeId = "scope";
        roslynAnalyzerService.ShouldUseEnterpriseCSharpAnalyzerAsync(Arg.Is<ShouldUseEnterpriseCSharpAnalyzerParams>(s => s.configurationScopeId == configurationScopeId))
            .Returns(new ShouldUseEnterpriseCSharpAnalyzerResponse(toReturn));
        var result = await testSubject.ShouldUseEnterpriseCSharpAnalyzerAsync(configurationScopeId);

        result.Should().Be(toReturn);
        testLogger.AssertNoOutputMessages();
    }
}
