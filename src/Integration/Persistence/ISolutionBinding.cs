//-----------------------------------------------------------------------
// <copyright file="ISolutionBinding.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Persistence
{
    // Test only interface
    internal interface ISolutionBinding
    {
        BoundSonarQubeProject ReadSolutionBinding();

        string WriteSolutionBinding(ISourceControlledFileSystem sccFileSystem, BoundSonarQubeProject binding);
    }
}
