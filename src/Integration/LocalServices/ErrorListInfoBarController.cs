//-----------------------------------------------------------------------
// <copyright file="ErrorListInfoBarController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration
{
    internal class ErrorListInfoBarController : IErrorListInfoBarController
    {
        private readonly IHost host;

        public ErrorListInfoBarController(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;
        }

        #region IErrorListInfoBarController
        public void Reset()
        {
            // TBD
        }

        public void Refresh()
        {
            // TBD
        }
        #endregion

        #region Non-public API

        #endregion
    }
}
