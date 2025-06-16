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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class AnalysisListener(
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IAnalysisRequester analysisRequester,
    IRaisedFindingProcessor raisedFindingProcessor,
    IIssuePublisher issuePublisher,
    IHotspotPublisher hotspotPublisher,
    ILogger logger)
    : IAnalysisListener
{
    public void DidChangeAnalysisReadiness(DidChangeAnalysisReadinessParams parameters)
    {
        var configScopeId = parameters.configurationScopeIds.Single();

        if (activeConfigScopeTracker.TryUpdateAnalysisReadinessOnCurrentConfigScope(configScopeId, parameters.areReadyForAnalysis))
        {
            logger.WriteLine(SLCoreStrings.AnalysisReadinessUpdate, parameters.areReadyForAnalysis);
            if (parameters.areReadyForAnalysis)
            {
                analysisRequester.RequestAnalysis();
            }
        }
        else
        {
            logger.WriteLine(SLCoreStrings.AnalysisReadinessUpdate, SLCoreStrings.ConfigScopeConflict);
        }
    }

    public void RaiseIssues(RaiseFindingParams<RaisedIssueDto> parameters) => raisedFindingProcessor.RaiseFinding(parameters, issuePublisher);

    public void RaiseHotspots(RaiseHotspotParams parameters) => raisedFindingProcessor.RaiseFinding(parameters, hotspotPublisher);
}
