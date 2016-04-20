//-----------------------------------------------------------------------
// <copyright file="IErrorListInfoBarController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration
{
    internal interface IErrorListInfoBarController : ILocalService
    {
        /// <summary>
        /// Checks whether the error list info bar needs to be displayed
        /// </summary>
        void Refresh();

        /// <summary>
        /// Detaches the info bar.
        /// Any state/events handling that may have configured for the info bar handling will be cleared
        /// </summary>
        void Reset();
    }
}
