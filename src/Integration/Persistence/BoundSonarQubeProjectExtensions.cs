//-----------------------------------------------------------------------
// <copyright file="BoundSonarQubeProjectExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using System;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal static class BoundSonarQubeProjectExtensions
    {
        public static ConnectionInformation CreateConnectionInformation(this BoundSonarQubeProject binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            return binding.Credentials == null ?
               new ConnectionInformation(binding.ServerUri)
               : binding.Credentials.CreateConnectionInformation(binding.ServerUri);
        }
    }
}
