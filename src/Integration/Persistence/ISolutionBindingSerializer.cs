//-----------------------------------------------------------------------
// <copyright file="ISolutionBinding.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Persistence
{
    // Test interface
    internal interface ISolutionBindingSerializer : ILocalService
    {
        /// <summary>
        /// Retrieves solution binding information
        /// </summary>
        /// <returns>Can be null if not bound</returns>
        BoundSonarQubeProject ReadSolutionBinding();

        /// <summary>
        /// Writes the binding information
        /// </summary>
        /// <param name="binding">Required</param>
        /// <param name="sccFileSystem">Required</param>
        /// <returns>The file path to the binding file</returns>
        string WriteSolutionBinding(BoundSonarQubeProject binding);
    }
}
