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

using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

public sealed class SLCoreTestRunner : IDisposable
{
    private SLCoreTestProcessRunner processRunner;
    private ILogger logger;
    private List<ISLCoreListener> listenersToSetUp = new();
    private readonly ClientConstantsDto defaultClientConstants = new("TEST", "TEST");
    private readonly TelemetryClientConstantAttributesDto defaultTelemetryAttributes = new("TEST", "TEST", "TEST", "TEST", new Dictionary<string, object>());
    private readonly List<Language> defaultEnabledLanguages = new()
    {
        Language.C,
        Language.CPP,
        Language.CS,
        Language.VBNET,
        Language.JS,
        Language.TS,
        Language.CSS,
        Language.SECRETS,
    };
    public SLCoreServiceProvider SlCoreServiceProvider { get; private set; }

    public SLCoreTestRunner(ILogger logger)
    {
        this.logger = logger;
    }

    public void AddListener(ISLCoreListener listener)
    {
        listenersToSetUp.Add(listener);
    }

    public void Start()
    {
        var (storageRoot, workDir, userHome) = SetUpLocalFolders();
        
        // todo replace path after the download problem is solved
        processRunner = new SLCoreTestProcessRunner(@"C:\Users\georgii.borovinskikh\Desktop\SLCORE\bin\sonarlint-backend.bat");

        SlCoreServiceProvider = new SLCoreServiceProvider(new NoOpThreadHandler(), logger);
        var slCoreListenerSetUp = new SLCoreListenerSetUp(listenersToSetUp);
        SlCoreServiceProvider.SetCurrentConnection(processRunner.Rpc);
        slCoreListenerSetUp.Setup(processRunner.Rpc);

        if (!SlCoreServiceProvider.TryGetTransientService(out ISLCoreLifecycleService slCoreLifecycleService))
        {
            throw new InvalidOperationException("Can't start SLOOP");
        }

        slCoreLifecycleService.InitializeAsync(
            new InitializeParams(
                defaultClientConstants,
                new FeatureFlagsDto(true, true, false, true, false, false, true),
                storageRoot,
                workDir,
                LoadAvailablePlugins(),
                new Dictionary<string, string>(),
                defaultEnabledLanguages,
                new List<Language>(),
                new List<SonarQubeConnectionConfigurationDto>(),
                new List<SonarCloudConnectionConfigurationDto>(),
                userHome,
                new Dictionary<string, StandaloneRuleConfigDto>(),
                false,
                defaultTelemetryAttributes,
                null));
    }

    public void Dispose()
    {
        if (SlCoreServiceProvider?.TryGetTransientService(out ISLCoreLifecycleService slCoreLifecycleService) ?? false)
        {
            slCoreLifecycleService.ShutdownAsync().GetAwaiter().GetResult();
        }
        
        processRunner?.Dispose();
    }
    
    private static (string storageRoot, string workDir, string userHome) SetUpLocalFolders()
    {
        var privateFolderBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "slcore");
        var storageRoot = Path.Combine(privateFolderBase, "storageRoot");
        var workDir = Path.Combine(privateFolderBase, "workDir");
        var userHome = Path.Combine(privateFolderBase, "userHome");

        if (Directory.Exists(privateFolderBase))
        {
            Directory.Delete(privateFolderBase, true);
        }

        Directory.CreateDirectory(privateFolderBase);
        Directory.CreateDirectory(storageRoot);
        Directory.CreateDirectory(workDir);
        Directory.CreateDirectory(userHome);

        return (storageRoot, workDir, userHome);
    }

    private static List<string> LoadAvailablePlugins()
    {
        return new List<string>
        {
            // todo replace with proper implementation
            @"C:\Users\georgii.borovinskikh\AppData\Local\SLVS_Build_DownloadedJars\sonar-javascript-plugin-10.12.0.25537.jar"
        };
    }
}
