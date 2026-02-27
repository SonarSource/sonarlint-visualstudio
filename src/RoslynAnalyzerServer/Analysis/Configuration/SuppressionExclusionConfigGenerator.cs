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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

[Export(typeof(ISuppressionExclusionConfigGenerator))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SuppressionExclusionConfigGenerator : ISuppressionExclusionConfigGenerator
{
    private readonly ILogger logger;
    internal const string ConfigFileName = "suppression_exclusions.globalconfig";
    internal readonly string ConfigFilePath;
    private readonly IRoslynAnalyzerAssemblyContentsLoader roslynAnalyzerAssemblyContentsLoader;
    private readonly IFileSystemService fileSystemService;

    [method: ImportingConstructor]
    public SuppressionExclusionConfigGenerator(IRoslynAnalyzerAssemblyContentsLoader roslynAnalyzerAssemblyContentsLoader,
        IEnvironmentVariableProvider environmentVariableProvider,
        IFileSystemService fileSystemService,
        IInitializationProcessorFactory initializationProcessorFactory,
        ILogger logger)
    {
        this.roslynAnalyzerAssemblyContentsLoader = roslynAnalyzerAssemblyContentsLoader;
        this.fileSystemService = fileSystemService;
        this.logger = logger.ForContext(Resources.SuppressionExclusionConfigGenerator_LogContext);
        ConfigFilePath = Path.Combine(environmentVariableProvider.GetSLVSAppDataRootPath(), ConfigFileName);

        InitializationProcessor = initializationProcessorFactory.CreateAndStart<SuppressionExclusionConfigGenerator>([roslynAnalyzerAssemblyContentsLoader], GenerateConfiguration);
    }

    internal void GenerateConfiguration()
    {
        try
        {
            var ruleKeys = roslynAnalyzerAssemblyContentsLoader.GetAllSupportedRuleKeys();
            var configContent = BuildConfigContent(ruleKeys);
            fileSystemService.Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            logger.LogVerbose(Resources.SuppressionExclusionConfigGenerator_WritingFile);
            fileSystemService.File.WriteAllText(ConfigFilePath, configContent);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.SuppressionExclusionConfigGenerator_FailedToWrite, ex);
        }
    }

    private static string BuildConfigContent(ImmutableHashSet<string> ruleKeys)
    {
        var ruleKeysList = string.Join(",", ruleKeys);

        return $"""
                is_global = true
                global_level = 1999999999

                dotnet_remove_unnecessary_suppression_exclusions = {ruleKeysList}

                """;
    }

    public IInitializationProcessor InitializationProcessor { get; }
}
