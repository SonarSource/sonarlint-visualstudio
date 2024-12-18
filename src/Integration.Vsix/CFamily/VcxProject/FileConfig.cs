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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    internal class FileConfig : IFileConfig
    {
        [ExcludeFromCodeCoverage]
        public static FileConfig TryGet(ILogger logger, ProjectItem dteProjectItem, string absoluteFilePath, IFileSystem fileSystem)
        {
            if (!(dteProjectItem.ContainingProject.Object is VCProject vcProject) ||
                !(dteProjectItem.Object is VCFile vcFile))
            {
                return null;
            }

            var vcConfig = vcProject.ActiveConfiguration;

            var vcFileSettings = GetVcFileSettings(logger, absoluteFilePath, vcConfig, vcFile);
            if (vcFileSettings == null)
            {
                // Not supported
                return null;
            }

            bool isHeaderFile = vcFile.ItemType == "ClInclude";
            CmdBuilder cmdBuilder = new CmdBuilder(isHeaderFile);

            if (!GetCompilerPath(logger, vcConfig, fileSystem, out var compilerPath))
            {
                return null;
            }
            logger.WriteLine(compilerPath);
            // command: add compiler
            cmdBuilder.AddCompiler(compilerPath);

            // command: add options from VCRulePropertyStorage
            cmdBuilder.AddOptFromProperties(vcFileSettings);

            // cmd add File
            cmdBuilder.AddFile(absoluteFilePath);
            var envINCLUDE = vcConfig.GetEvaluatedPropertyValue("IncludePath");

            return new FileConfig
            {
                CDDirectory = Path.GetDirectoryName(vcProject.ProjectFile),
                CDCommand = cmdBuilder.GetFullCmd(),
                CDFile = absoluteFilePath,
                EnvInclude = envINCLUDE,
                IsHeaderFile = isHeaderFile,
            };
        }

        private static bool TryGetCompilerPathFromClCompilerPath(ILogger logger, VCConfiguration vcConfig, IFileSystem fileSystem, out string compilerPath)
        {
            compilerPath = vcConfig.GetEvaluatedPropertyValue("ClCompilerPath");
            if (string.IsNullOrEmpty(compilerPath))
            {
                logger.WriteLine("\"ClCompilerPath\" was not found.");
                return false;
            }

            if (!fileSystem.File.Exists(compilerPath))
            {
                logger.WriteLine($"Compiler path (based on \"ClCompilerPath\") \"{compilerPath}\" does not exist.");
                return false;
            }

            return true;
        }

        private static bool TryGetCompilerPathFromExecutablePath(ILogger logger, VCConfiguration vcConfig, IFileSystem fileSystem, out string compilerPath)
        {
            compilerPath = default;
            var executablePath = vcConfig.GetEvaluatedPropertyValue("ExecutablePath");
            if (string.IsNullOrEmpty(executablePath))
            {
                logger.WriteLine("\"ExecutablePath\" was not found.");
                return false;
            }

            var toolExe = vcConfig.GetEvaluatedPropertyValue("CLToolExe");
            if (string.IsNullOrEmpty(toolExe))
            {
                // VS2022 sets "CLToolExe" only when clang-cl is chosen as the toolset.
                logger.WriteLine("\"CLToolExe\" was not found, falling back to cl.exe.");
                toolExe = "cl.exe";
            }

            foreach (var path in executablePath.Split(';'))
            {
                compilerPath = Path.Combine(path, toolExe);
                if (fileSystem.File.Exists(compilerPath))
                {
                    return true;
                }
                else
                {
                    logger.WriteLine($"Compiler path (based on \"ExecutablePath\") \"{compilerPath}\" does not exist.");
                }
            }

            return false;
        }

        private static bool TryGetCompilerPathFromVCExecutablePath(ILogger logger, VCConfiguration vcConfig, IFileSystem fileSystem, out string compilerPath)
        {
            var platform = ((VCPlatform)vcConfig.Platform).Name.Contains("64") ? "x64" : "x86";
            var exeVar = "VC_ExecutablePath_" + platform;
            compilerPath = Path.Combine(vcConfig.GetEvaluatedPropertyValue(exeVar), "cl.exe");
            if (fileSystem.File.Exists(compilerPath))
            {
                return true;
            }
            else
            {
                logger.WriteLine($"Compiler path (based on \"{exeVar}\") \"{compilerPath}\" does not exist.");
                return false;
            }
        }

        private static bool GetCompilerPath(ILogger logger, VCConfiguration vcConfig, IFileSystem fileSystem, out string compilerPath)
        {
            if (TryGetCompilerPathFromClCompilerPath(logger, vcConfig, fileSystem, out compilerPath))
            {
                return true;
            }

            // Fallback to ExecutablePath and CLToolExe
            if (TryGetCompilerPathFromExecutablePath(logger, vcConfig, fileSystem, out compilerPath))
            {
                return true;
            }

            // Fallback to VC_ExecutablePath, which is used to be used in VS2017 toolchains
            // In case ClCompilerPath isn't available, and ExecutablePath matching fails
            if (TryGetCompilerPathFromVCExecutablePath(logger, vcConfig, fileSystem, out compilerPath))
            {
                return true;
            }

            logger.WriteLine("Compiler is not supported.");
            return false;
        }

        private static IVCRulePropertyStorage GetVcFileSettings(ILogger logger, string absoluteFilePath, VCConfiguration vcConfig, VCFile vcFile)
        {
            var projectKind = vcConfig.ConfigurationType;

            // Unknown projects represent all the unsupported project type like makefile projects
            if (projectKind == ConfigurationTypes.typeUnknown)
            {
                logger.WriteLine(@"Project's ""Configuration type"" is not supported.");
                return null;
            }

            // "ClCompile" for source files and "ClInclude" for header files
            if (vcFile.ItemType != "ClCompile" && vcFile.ItemType != "ClInclude")
            {
                logger.WriteLine($"File's \"Item type\" is not supported. File: '{absoluteFilePath}'");
                return null;
            }

            object toolItem;

            if (vcFile.ItemType == "ClCompile")
            {
                var vcFileConfig = vcFile.GetFileConfigurationForProjectConfiguration(vcConfig);
                toolItem = vcFileConfig.Tool;
            }
            else
            {
                var tools = vcConfig.Tools as IVCCollection;
                toolItem = tools.Item("VCCLCompilerTool");
            }

            // We don't support custom build tools. VCCLCompilerTool is needed for all the necessary compilation options to be present
            if (!(toolItem is VCCLCompilerTool))
            {
                logger.WriteLine($"Custom build tools aren't supported. Custom-built file: '{absoluteFilePath}'");
                return null;
            }

            // The Tool property is typed as Microsoft.VisualStudio.VCProjectEngine.VCCLCompilerTool, and
            // we could use it to fetch most of the properties we need, rather than fetching them via the
            // IVCRulePropertyStorage interface. However, not all of the properties are exposed directly by
            // VCCLCompilerTool (e.g. LanguageStandard). Also, quite a few of the Tool properties are exposed
            // as enums, so we'd need to change our code to handle them.
            var vcFileSettings = toolItem as IVCRulePropertyStorage;

            return vcFileSettings;
        }

        #region Properties

        public string CDDirectory { get; set; }
        public string CDCommand { get; set; }
        public string CDFile { get; set; }
        public string EnvInclude { get; set; }
        public bool IsHeaderFile { get; set; }

        #endregion

    }
}
