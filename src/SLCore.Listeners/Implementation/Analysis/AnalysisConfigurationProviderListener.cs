﻿/*
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class AnalysisConfigurationProviderListener(IActiveConfigScopeTracker activeConfigScopeTracker, ILogger logger) : IAnalysisConfigurationProviderListener
{
    private readonly ILogger analysisConfigLogger = logger.ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.SLCoreAnalysisConfigurationLogContext);

    public Task<GetBaseDirResponse> GetBaseDirAsync(GetBaseDirParams parameters)
    {
        string baseDir;
        var currentConfigurationScope = activeConfigScopeTracker.Current;

        if (currentConfigurationScope is not null && currentConfigurationScope.Id == parameters.configurationScopeId)
        {
            baseDir = currentConfigurationScope.CommandsBaseDir;
        }
        else
        {
            analysisConfigLogger.WriteLine(SLCoreStrings.ConfigurationScopeMismatch, parameters.configurationScopeId, currentConfigurationScope?.Id);
            baseDir = null;
        }

        return Task.FromResult(new GetBaseDirResponse(baseDir));
    }

    public Task<GetInferredAnalysisPropertiesResponse> GetInferredAnalysisPropertiesAsync(GetInferredAnalysisPropertiesParams parameters) =>
        Task.FromResult(new GetInferredAnalysisPropertiesResponse([]));
}
