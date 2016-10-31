//-----------------------------------------------------------------------
// <copyright file="ConnectionInfoDialogViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Security;

namespace SonarLint.VisualStudio.Integration.Connection.UI
{
    internal class ConnectionInfoDialogViewModel : NotifyErrorViewModelBase
    {
        private readonly UriValidator uriValidator;
        private readonly BasicAuthenticationCredentialsValidator credentialsValidator;

        internal /* testing purposes */ bool IsUrlPristine
        {
            get; private set;
        } = true;
        private string serverUrlRaw;
        private string username;

        public ConnectionInfoDialogViewModel()
            : this(new UriValidator(), new BasicAuthenticationCredentialsValidator())
        {
        }

        internal /* testing purposes */ ConnectionInfoDialogViewModel(UriValidator uriValidator, BasicAuthenticationCredentialsValidator credentialsValidator)
        {
            this.uriValidator = uriValidator;
            this.credentialsValidator = credentialsValidator;
        }

        #region Properties

        public string ServerUrlRaw
        {
            get { return this.serverUrlRaw; }
            set
            {
                this.IsUrlPristine = false;
                SetAndRaisePropertyChanged(ref this.serverUrlRaw, value);
                RaisePropertyChanged(nameof(this.ServerUrl));
                RaisePropertyChanged(nameof(this.IsServerUrlValid));
                RaisePropertyChanged(nameof(this.ShowSecurityWarning));
                RaisePropertyChanged(nameof(this.IsValid));
            }
        }

        public Uri ServerUrl
        {
            get
            {
                if (this.uriValidator.IsValidUri(this.ServerUrlRaw))
                {
                    return new Uri(this.ServerUrlRaw);
                }
                return null;
            }
        }

        public string Username
        {
            get { return this.username; }
            set
            {
                SetAndRaisePropertyChanged(ref this.username, value);
                RaisePropertyChanged(nameof(this.IsValid));
            }
        }

        public bool ShowSecurityWarning
        {
            get
            {
                return this.ServerUrl != null
                    && this.uriValidator.IsInsecureScheme(this.ServerUrl);
            }
        }

        public bool IsServerUrlValid
        {
            get { return this.uriValidator.IsValidUri(this.ServerUrlRaw); }
        }

        /// <summary>
        /// Whether or not all credential information is valid.
        /// </summary>
        public bool IsCredentialsValid
        {
            get { return this.credentialsValidator.IsValid; }
        }

        /// <summary>
        /// Whether or not the entire model is valid.
        /// </summary>
        /// <remarks>Also includes credentials validation status (<see cref="IsCredentialsValid"/>)</remarks>
        public bool IsValid
        {
            get { return this.IsServerUrlValid && this.IsCredentialsValid; }
        }

        public override bool HasErrors
        {
            get { return !this.IsValid; }
        }

        #endregion

        /// <summary>
        /// Cause validation to occur for the credentials.
        /// </summary>
        /// <param name="password"><see cref="SecureString"/> password</param>
        public void ValidateCredentials(SecureString password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            Debug.Assert(password.Length >= 0, "Password length shouldn't be negative");

            this.credentialsValidator.Update(this.Username, password);

            // Notify of possible changes to validation errors for Username and Password
            this.RaisePropertyChanged(nameof(this.IsCredentialsValid));
            this.RaisePropertyChanged(nameof(this.IsValid));
            this.RaiseErrorsChanged(nameof(this.Username));
        }

        protected override bool GetErrorForProperty(string propertyName, ref string error)
        {
            if (propertyName == nameof(this.ServerUrlRaw) && !this.IsUrlPristine && !this.IsServerUrlValid)
            {
                error = string.Format(CultureInfo.CurrentCulture, Resources.Strings.InvalidServerUriFormat, this.ServerUrlRaw);
                return true;
            }

            if (propertyName == nameof(this.Username) && !this.credentialsValidator.IsUsernameValid)
            {
                error = this.credentialsValidator.InvalidUsernameErrorMessage;
                return true;
            }

            return false;
        }
    }
}
