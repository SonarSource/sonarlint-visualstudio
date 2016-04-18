//-----------------------------------------------------------------------
// <copyright file="IInfoBar.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration.InfoBar
{
    /// <summary>
    /// Represents an attached info bar
    /// </summary>
    public interface IInfoBar
    {
        /// <summary>
        /// Click event
        /// </summary>
        event EventHandler ButtonClick;

        /// <summary>
        /// Closed event
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// Close the info bar
        /// </summary>
        void Close();
    }
}
