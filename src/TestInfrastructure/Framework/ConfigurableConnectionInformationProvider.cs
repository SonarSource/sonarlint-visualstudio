//-----------------------------------------------------------------------
// <copyright file="ConfigurableConnectionInformationProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableConnectionInformationProvider : IConnectionInformationProvider
    {
        #region IConnectionInformationProvider
        ConnectionInformation IConnectionInformationProvider.GetConnectionInformation(ConnectionInformation currentConnection)
        {
            if (this.ExpectExistingConnection)
            {
                Assert.IsNotNull(currentConnection, "No existing connection provided");
            }
            return this.ConnectionInformationToReturn;
        }
        #endregion

        #region Test helpers
        public bool ExpectExistingConnection
        {
            get; set;
        }

        public ConnectionInformation ConnectionInformationToReturn
        {
            get;
            set;
        }
        #endregion
    }
}
