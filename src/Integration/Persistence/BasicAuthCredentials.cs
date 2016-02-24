//-----------------------------------------------------------------------
// <copyright file="BasicAuthCredentials.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Security;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class BasicAuthCredentials : ICredentials
    {
        public BasicAuthCredentials(string userName, SecureString password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        public string UserName { get; }

        public SecureString Password { get; }

        ConnectionInformation ICredentials.CreateConnectionInformation(Uri serverUri)
        {
            return new ConnectionInformation(serverUri, this.UserName, this.Password);
        }
    }
}
