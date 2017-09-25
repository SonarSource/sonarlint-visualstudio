/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    // TODO:
    // * needs a connection to the current SonarQube server
    // * maintain a list of server-side issues
    // * update the list periodically - triggers?
    // * provide fast access to locate issues by project and file
    // * locking?

    public sealed class ServerIssuesProvider : IServerIssuesProvider, IDisposable
    {
        private readonly IServiceProvider serviceProvider;

        public ServerIssuesProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            SetupRefreshTriggers();
        }

        public IEnumerable<ServerIssue> GetServerIssues(string projectId, string filePath)
        {
            // TODO: if the issues have not been fetch yet, block until they are fetched (or error/timeout)
            // TODO: locking
            return Enumerable.Empty<ServerIssue>();
        }

        private void SetupRefreshTriggers()
        {
            // TODO: set up triggers to re-fetch the issues from the server

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
