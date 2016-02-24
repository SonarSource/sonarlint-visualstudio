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
        private Dictionary<Uri, Credential> data = new Dictionary<Uri, Credential>();

        #region ICredentialStore
        void ICredentialStore.DeleteCredentials(Uri targetUri)
        {
            this.data.Remove(targetUri);
        }

        bool ICredentialStore.ReadCredentials(Uri targetUri, out Credential credentials)
        {
            return this.data.TryGetValue(targetUri, out credentials);
        }

        void ICredentialStore.WriteCredentials(Uri targetUri, Credential credentials)
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
