//-----------------------------------------------------------------------
// <copyright file="ConnectionInformation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Security;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// Represents the connection information needed to connect to SonarQube service
    /// </summary>
    internal class ConnectionInformation : ICloneable, IDisposable
    {
        private bool isDisposed;

        internal ConnectionInformation(Uri serverUri, string userName, SecureString password)
        {
            if (serverUri == null)
            {
                throw new ArgumentNullException(nameof(serverUri));
            }

            this.ServerUri = serverUri.EnsureTrailingSlash();
            this.UserName = userName;
            this.Password = password?.CopyAsReadOnly();
            this.Authentication = AuthenticationType.Basic; // Only one supported at this point
        }

        internal ConnectionInformation(Uri serverUri)
            : this(serverUri, null, null)
        {
        }

        public Uri ServerUri
        {
            get;
        }

        public string UserName
        {
            get;
        }

        public SecureString Password
        {
            get;
        }

        public AuthenticationType Authentication
        {
            get;
        }

        internal /*for testing purposes*/ bool IsDisposed
        {
            get
            {
                return this.isDisposed;
            }
        }

        public ConnectionInformation Clone()
        {
            return new ConnectionInformation(this.ServerUri, this.UserName, this.Password?.CopyAsReadOnly());
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.Password?.Dispose();
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }
        #endregion
    }
}
