//-----------------------------------------------------------------------
// <copyright file="ConfigurableCredentialStore.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableCredentialStore : ICredentialStore
    {
        private readonly Dictionary<Uri, Credential> data = new Dictionary<Uri, Credential>();

        public string Namespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Secret.UriNameConversion UriNameConversion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #region ICredentialStore
        void ICredentialStore.DeleteCredentials(TargetUri targetUri)
        {
            this.data.Remove(targetUri);
        }

        Credential ICredentialStore.ReadCredentials(TargetUri targetUri)
        {
            Credential credential;
            return this.data.TryGetValue(targetUri, out credential) ? credential : null;
        }

        void ICredentialStore.WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            this.data[targetUri] = credentials;
        }
        #endregion

        #region Helpers
        public void AssertHasCredentials(Uri targetUri)
        {
            Assert.IsTrue(this.data.ContainsKey(targetUri), "Credentials not found for uri {0}", targetUri);
        }

        public void AssertHasNoCredentials(Uri targetUri)
        {
            Assert.IsFalse(this.data.ContainsKey(targetUri), "Credentials found for uri {0}", targetUri);
        }
        #endregion
    }
}
