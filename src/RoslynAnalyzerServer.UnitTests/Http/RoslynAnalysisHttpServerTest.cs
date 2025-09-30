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
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Http;

[TestClass]
public class RoslynAnalysisHttpServerTest
{
    private static IHttpServerConfigurationFactory _serverConfigurationFactory = null!;
    private static IHttpRequestHandler _httpRequestHandler = null!;
    private static ILogger _logger = null!;
    private static IHttpServerSettings _configuration = null!;
    private static IAnalysisRequestHandler _analysisRequestHandler = null!;
    private static IRoslynAnalysisService _roslynAnalysisService = null!;
    private static RoslynAnalysisHttpServer _testSubject = null!;

    [ClassInitialize]
    public static void TestInitialize(TestContext context)
    {
        _logger = Substitute.For<ILogger>();
        _logger.ForContext(Arg.Any<string[]>()).Returns(_logger);
        _configuration = Substitute.For<IHttpServerSettings>();
        _analysisRequestHandler = Substitute.For<IAnalysisRequestHandler>();
        _httpRequestHandler = Substitute.For<IHttpRequestHandler>();
        _roslynAnalysisService = Substitute.For<IRoslynAnalysisService>();
        _serverConfigurationFactory = Substitute.For<IHttpServerConfigurationFactory>();
        _testSubject = new RoslynAnalysisHttpServer(_logger, _configuration, _analysisRequestHandler, _httpRequestHandler, new HttpListenerFactory(),
            _serverConfigurationFactory, _roslynAnalysisService);
    }

    [ClassCleanup]
    public static void TestCleanup() => _testSubject.Dispose();

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalysisHttpServer, IRoslynAnalysisHttpServer>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IHttpServerSettings>(),
            MefTestHelpers.CreateExport<IAnalysisRequestHandler>(),
            MefTestHelpers.CreateExport<IHttpRequestHandler>(),
            MefTestHelpers.CreateExport<IHttpListenerFactory>(),
            MefTestHelpers.CreateExport<IHttpServerConfigurationFactory>(),
            MefTestHelpers.CreateExport<IRoslynAnalysisService>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalysisHttpServer>();

    [TestMethod]
    public void Ctor_LoggerSetsContext() => _logger.Received(1).ForContext(Resources.HttpServerLogContext).ForContext(nameof(RoslynAnalysisHttpServer));

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _testSubject.Dispose();
        _testSubject.Dispose();

        _logger.Received(1).LogVerbose(Resources.HttpServerDisposed);
    }
}
