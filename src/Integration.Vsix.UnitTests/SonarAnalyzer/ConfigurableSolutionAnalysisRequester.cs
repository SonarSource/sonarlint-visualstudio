//-----------------------------------------------------------------------
// <copyright file="ConfigurableSolutionAnalysisRequester.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    internal class ConfigurableSolutionAnalysisRequester : ISolutionAnalysisRequester
    {
        public int ReanalyzeSolutionCallCount { get; set; }

        public void ReanalyzeSolution()
        {
            this.ReanalyzeSolutionCallCount++;
        }
    }
}
