/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Helpers;
using VsShell = Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    [Export(typeof(IRequestFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VcxRequestFactory : IRequestFactory
    {
        private readonly ICFamilyRulesConfigProvider cFamilyRulesConfigProvider;
        private readonly IRulesConfigProtocolFormatter rulesConfigProtocolFormatter;
        private readonly IThreadHandling threadHandling;
        private readonly IFileConfigProvider fileConfigProvider;
        private readonly ILogger logger;
        private readonly DTE2 dte;

        [ImportingConstructor]
        public VcxRequestFactory([Import(typeof(VsShell.SVsServiceProvider))] IServiceProvider serviceProvider,
            ICFamilyRulesConfigProvider cFamilyRulesConfigProvider,
            ILogger logger)
            : this(serviceProvider,
                cFamilyRulesConfigProvider,
                new RulesConfigProtocolFormatter(),
                new FileConfigProvider(logger),
                logger,
                new ThreadHandling())
        {
        }

        internal VcxRequestFactory(IServiceProvider serviceProvider,
            ICFamilyRulesConfigProvider rulesConfigProvider,
            IRulesConfigProtocolFormatter rulesConfigProtocolFormatter,
            IFileConfigProvider fileConfigProvider,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.dte = serviceProvider.GetService<SDTE, DTE2>();
            this.cFamilyRulesConfigProvider = rulesConfigProvider;
            this.rulesConfigProtocolFormatter = rulesConfigProtocolFormatter;
            this.threadHandling = threadHandling;
            this.fileConfigProvider = fileConfigProvider;
            this.logger = logger;
        }

        public async Task<IRequest> TryCreateAsync(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            threadHandling.ThrowIfOnUIThread();

            try
            {
                LogDebug("Trying to create request for " + analyzedFilePath);

                IFileConfig fileConfig = null;

                await threadHandling.RunOnUIThread(() =>
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

            var projectItem = dte?.Solution?.FindProjectItem(analyzedFilePath);

            if (projectItem == null)
            {
                LogDebug("\tCould not locate a project item");
                return null;
            }

            var fileConfig = fileConfigProvider.Get(projectItem, analyzedFilePath, analyzerOptions);

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

            var request = ToRequest(fileConfig, analyzedFilePath);

            if (request.CFamilyLanguage == null)
            {
                return null;
            }

            // TODO - remove File, PchFile and CFamilyLanguage from Request (both on RequestContext)
            request.PchFile = SubProcessFilePaths.PchFilePath;
            request.FileConfig = fileConfig;

            bool isPCHBuild = false;

            if (analyzerOptions is CFamilyAnalyzerOptions cFamilyAnalyzerOptions)
            {
                Debug.Assert(
                    !(cFamilyAnalyzerOptions.CreateReproducer && cFamilyAnalyzerOptions.CreatePreCompiledHeaders),
                    "Only one flag (CreateReproducer, CreatePreCompiledHeaders) can be set at a time");

                if (cFamilyAnalyzerOptions.CreateReproducer)
                {
                    request.Flags |= Request.CreateReproducer;
                }

                if (cFamilyAnalyzerOptions.CreatePreCompiledHeaders)
                {
                    request.Flags |= Request.BuildPreamble;
                    isPCHBuild = true;
                }
            }

            ICFamilyRulesConfig rulesConfig = null;
            if (!isPCHBuild)
            {
                rulesConfig = cFamilyRulesConfigProvider.GetRulesConfiguration(request.CFamilyLanguage);
                Debug.Assert(rulesConfig != null, "RulesConfiguration should be have been retrieved");

                // We don't need to calculate / set the rules configuration for PCH builds
                var protocolFormat = rulesConfigProtocolFormatter.Format(rulesConfig);
                protocolFormat.RuleParameters.Add("internal.qualityProfile", protocolFormat.QualityProfile);

                request.Options = protocolFormat.RuleParameters.Select(kv => kv.Key + "=" + kv.Value).ToArray();
            }

            var context = new RequestContext(request.CFamilyLanguage, rulesConfig, request.File,
                SubProcessFilePaths.PchFilePath, analyzerOptions);

            request.Context = context;

            return request;
        }

        private static Request ToRequest(IFileConfig fileConfig, string path)
        {
            var c = CFamilyHelper.Capture.ToCaptures(fileConfig, path, out string cfamilyLanguage);
            var request = MsvcDriver.ToRequest(c);
            request.CFamilyLanguage = cfamilyLanguage;

            if (fileConfig.ItemType == "ClInclude")
            {
                request.Flags |= Request.MainFileIsHeader;
            }
            return request;
        }

        private void LogDebug(string message)
        {
            logger.LogDebug($"[VCX:VcxRequestFactory] [Thread id: {System.Threading.Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}
