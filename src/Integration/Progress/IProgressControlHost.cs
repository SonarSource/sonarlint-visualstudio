//-----------------------------------------------------------------------
// <copyright file="IProgressControlHost.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Progress
{
    public interface IProgressControlHost
    {
        /// <summary>
        /// Request to host the <see cref="ProgressControl"/> for display purposes
        /// </summary>
        /// <param name="progressControl">Required</param>
        void Host(ProgressControl progressControl);
    }
}
