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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Analysis.Models;

namespace SonarLint.VisualStudio.ConnectedMode.ConfigurationScope;

[Export(typeof(IConfigurationScopeDotnetAnalyzerIndicator))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class ConfigurationScopeDotnetAnalyzerIndicator(ISLCoreServiceProvider serviceProvider, ILogger log)
    : IConfigurationScopeDotnetAnalyzerIndicator
{
    public async Task<bool> ShouldUseEnterpriseCSharpAnalyzerAsync(string configurationScopeId)
    {
        try
        {
            if (configurationScopeId is null)
            {
                log.WriteLine(Resources.DotnetAnalyzerIndicatorTemplate, SLCoreStrings.ConfigScopeNotInitialized);
                return false;
            }
            
            if (!serviceProvider.TryGetTransientService(out IRoslynAnalyzerService analyzerService))
            {
                log.WriteLine(Resources.DotnetAnalyzerIndicatorTemplate, SLCoreStrings.ServiceProviderNotInitialized);
                return false;
            }
            
            var response = await analyzerService.ShouldUseEnterpriseCSharpAnalyzerAsync(
                new ShouldUseEnterpriseCSharpAnalyzerParams(configurationScopeId));
            return response.shouldUseEnterpriseAnalyzer;
        }
        catch (Exception e)
        {
            log.WriteLine(Resources.DotnetAnalyzerIndicatorTemplate, e);
            return false;
        }
    }
}
