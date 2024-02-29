/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Collections.Generic;
using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.UnitTests.Process;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Integration;

[TestClass]
public class SLCoreIntegrationSmokeTest
{
    [TestMethod]
    [Ignore("Smoke test. Replace in the future.")]
    public async Task SLCore_InitializeStandaloneAndShutdown()
    {
        const string slcoreBat = @"";
        const string storageRoot = @"";
        const string workDir = @"";
        const string userHome = @"";
        var embeddedPluginPaths = new List<string>
        {
            @""
        };

        var slCoreRunner = new SLCoreRunner(slcoreBat, enableVerboseLogs: true);
        var logger = new TestLogger();

        var slCoreServiceProvider = new SLCoreServiceProvider(new NoOpThreadHandler(), logger);
        slCoreServiceProvider.SetCurrentConnection(slCoreRunner.Rpc);
        //var slCoreListenerSetUp = new SLCoreListenerSetUp(new[] { new LoggerListener(new TestLogger(logToConsole: true)) });
        //slCoreListenerSetUp.Setup(slCoreRunner.Rpc);

        slCoreServiceProvider.TryGetTransientService(out ISLCoreLifecycleService slCoreLifecycleService).Should()
            .BeTrue();

        await slCoreLifecycleService.InitializeAsync(new InitializeParams(
            new ClientConstantsDto("TEST", "TEST"),
            new HttpConfigurationDto(new SslConfigurationDto()),
            new FeatureFlagsDto(false, false, false, false, false, false, false),
            storageRoot,
            workDir,
            embeddedPluginPaths,
            new Dictionary<string, string>(),
            new List<Language> { Language.JS },
            new List<Language>(),
            new List<SonarQubeConnectionConfigurationDto>(),
            new List<SonarCloudConnectionConfigurationDto>(),
            userHome,
            new Dictionary<string, StandaloneRuleConfigDto>
            {
                { "javascript:S1940", new StandaloneRuleConfigDto(true, new Dictionary<string, string>()) }
            },
            false,
            new TelemetryClientConstantAttributesDto("TEST", "TEST", "TEST", "TEST", new Dictionary<string, object>()),
            null
        ));

        await slCoreLifecycleService.ShutdownAsync();

        slCoreRunner.Dispose();
    }
}
