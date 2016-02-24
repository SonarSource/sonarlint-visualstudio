//-----------------------------------------------------------------------
// <copyright file="ConnectedProjectsCallback.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// Projects for connection information have changed
    /// </summary>
    /// <param name="connection">Not null</param>
    /// <param name="projects">Can be null when there are no projects</param>
    internal delegate void ConnectedProjectsCallback(
        ConnectionInformation connection,
        IEnumerable<ProjectInformation> projects);
}
