using System;
using System.Security;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Models
{
    // TODO: I want to use this class but there are a lot changes to do so keep using the other now on.
    //public class Connection
    //{
    //    public Uri ServerUri { get; set; }
    //    public string Login { get; set; }
    //    public SecureString Password { get; set; }
    //    public AuthenticationType Authentication { get; set; }
    //}

    /// <summary>
    /// Represents the connection information needed to connect to SonarQube service
    /// </summary>
    public class ConnectionInformation : ICloneable, IDisposable
    {
        private bool isDisposed;

        public ConnectionInformation(Uri serverUri, string userName, SecureString password)
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

        public ConnectionInformation(Uri serverUri)
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

        public Organization Organization { get; set; }

        public ConnectionInformation Clone()
        {
            return new ConnectionInformation(this.ServerUri, this.UserName, this.Password?.CopyAsReadOnly()) { Organization = Organization };
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

    public class ConnectionDTO
    {
        public Uri ServerUri { get; set; }
        public string Login { get; set; }
        public SecureString Password { get; set; }
        public AuthenticationType Authentication { get; set; }
    }
}
