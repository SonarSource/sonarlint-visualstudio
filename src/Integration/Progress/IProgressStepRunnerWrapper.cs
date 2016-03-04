//-----------------------------------------------------------------------
// <copyright file="IProgressStepRunnerWrapper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Progress
{
    // Test only interface to hide the implementation of ProgressStepRunner
    internal interface IProgressStepRunnerWrapper
    {
        void ChangeHost(IProgressControlHost host);

        void AbortAll();
    }
}
