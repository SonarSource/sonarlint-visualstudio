//-----------------------------------------------------------------------
// <copyright file="IWebBrowser.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration
{
    internal interface IWebBrowser
    {
        /// <summary>
        /// Opens the current user's preferred web browser at the provided URL.
        /// </summary>
        /// <param name="url">URL to navigate to</param>
        void NavigateTo(string url);
    }
}