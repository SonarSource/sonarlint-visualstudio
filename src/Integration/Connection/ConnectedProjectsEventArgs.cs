//-----------------------------------------------------------------------
// <copyright file="ConnectedProjectsEventArgs.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class ConnectedProjectsEventArgs : EventArgs
    {
        public ConnectedProjectsEventArgs(ConnectionInformation connection, IEnumerable<ProjectInformation> projects)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            this.Connection = connection;
            this.Projects = projects;
        }

        public ConnectionInformation Connection { get; }

        public IEnumerable<ProjectInformation> Projects { get; }
    }
}
