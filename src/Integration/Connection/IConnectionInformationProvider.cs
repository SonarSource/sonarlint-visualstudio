//-----------------------------------------------------------------------
// <copyright file="IConnectionInformationProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;

namespace SonarLint.VisualStudio.Integration.Connection
{
    // Test only interface
    internal interface IConnectionInformationProvider
    {
        /// <summary>
        /// Returns the connection information
        /// </summary>
        /// <param name="currentConnection">Optional, the current connection information (which might be invalid)</param>
        /// <returns></returns>
        ConnectionInformation GetConnectionInformation(ConnectionInformation currentConnection);
    }
}
