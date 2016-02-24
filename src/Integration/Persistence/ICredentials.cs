//-----------------------------------------------------------------------
// <copyright file="ICredentials.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using System;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal interface ICredentials
    {
        ConnectionInformation CreateConnectionInformation(Uri serverUri);
    }
}
