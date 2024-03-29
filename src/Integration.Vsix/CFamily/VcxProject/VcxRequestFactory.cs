﻿/*
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using VsShell = Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    [Export(typeof(IRequestFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VcxRequestFactory : IRequestFactory
    {
        private readonly ICFamilyRulesConfigProvider cFamilyRulesConfigProvider;
        private readonly IThreadHandling threadHandling;
        private readonly Lazy<IFileConfigProvider> fileConfigProvider;
        private readonly ILogger logger;
        private readonly Lazy<DTE2> dte;

        [ImportingConstructor]
        public VcxRequestFactory([Import(typeof(VsShell.SVsServiceProvider))] IServiceProvider serviceProvider,
            ICFamilyRulesConfigProvider cFamilyRulesConfigProvider,
            ILogger logger,
            IThreadHandling threadHandling)
// Suppress FP. The call to threadHandling.ThrowIfNotOnUIThread() inside the Lazy<> causes the
// incorrect identify this constructor as needing to do a threading check.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            : this(serviceProvider,
                cFamilyRulesConfigProvider,
                new Lazy<IFileConfigProvider>(() => new FileConfigProvider(logger)),
                logger,
                threadHandling)
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        {
        }

        internal VcxRequestFactory(IServiceProvider serviceProvider,
            ICFamilyRulesConfigProvider rulesConfigProvider,
            Lazy<IFileConfigProvider> fileConfigProvider,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.cFamilyRulesConfigProvider = rulesConfigProvider;
            this.threadHandling = threadHandling;
            this.fileConfigProvider = fileConfigProvider;
            this.logger = logger;

            this.dte = new Lazy<DTE2>(() =>
            {
                threadHandling.ThrowIfNotOnUIThread();
                return serviceProvider.GetService<SDTE, DTE2>();
            });
        }

        public async Task<IRequest> TryCreateAsync(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            threadHandling.ThrowIfOnUIThread();

            try
            {
                LogDebug("Trying to create request for " + analyzedFilePath);

                IFileConfig fileConfig = null;

                await threadHandling.RunOnUIThreadAsync(() =>
                {
                    fileConfig = GetFileConfigSync(analyzedFilePath, analyzerOptions);
                });

                if (fileConfig == null)
                {
                    return null;
                }

                var request = CreateRequest(analyzedFilePath, analyzerOptions, fileConfig);

                LogDebug("\tCreated request successfully");

                return request;
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CFamilyStrings.ERROR_CreatingVcxRequest, analyzedFilePath, ex);
                return null;
            }
        }

        private IFileConfig GetFileConfigSync(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            threadHandling.ThrowIfNotOnUIThread();

            var projectItem = dte?.Value.Solution?.FindProjectItem(analyzedFilePath);

            if (projectItem == null)
            {
                LogDebug("\tCould not locate a project item");
                return null;
            }

            var fileConfig = fileConfigProvider.Value.Get(projectItem, analyzedFilePath, analyzerOptions);

            if (fileConfig == null)
            {
                LogDebug("\tCould not get the file configuration");
                return null;
            }

            return fileConfig;
        }

        private IRequest CreateRequest(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions, IFileConfig fileConfig)
        {
            threadHandling.ThrowIfOnUIThread();

            var dbEntry = new CompilationDatabaseEntry { 
                Directory = fileConfig.CDDirectory,
                Command = fileConfig.CDCommand,
                File = fileConfig.CDFile,
                Arguments = null
            };

            var headerFileLang = fileConfig.HeaderFileLanguage == "cpp" ? SonarLanguageKeys.CPlusPlus : SonarLanguageKeys.C;
            var isHeaderFile = !string.IsNullOrEmpty(fileConfig.HeaderFileLanguage);
            var languageKey = isHeaderFile ? headerFileLang : CFamilyShared.FindLanguageFromExtension(dbEntry.File);

            if (languageKey == null)
            {
                return null;
            }
            ICFamilyRulesConfig rulesConfig = null;

            if (analyzerOptions == null || !analyzerOptions.CreatePreCompiledHeaders)
            {
                rulesConfig = cFamilyRulesConfigProvider.GetRulesConfiguration(languageKey);
            }

            var context = new RequestContext(
                languageKey, 
                rulesConfig, 
                analyzedFilePath, 
                SubProcessFilePaths.PchFilePath, 
                analyzerOptions,
                isHeaderFile);

            var envVars = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>() { 
                { "INCLUDE", fileConfig.EnvInclude } 
            });

            return new CompilationDatabaseRequest(dbEntry, context, envVars);
        }

        private void LogDebug(string message)
        {
            logger.LogVerbose($"[VCX:VcxRequestFactory] [Thread id: {System.Threading.Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}
