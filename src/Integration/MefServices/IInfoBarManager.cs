//-----------------------------------------------------------------------
// <copyright file="IInfoBarManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace SonarLint.VisualStudio.Integration.InfoBar
{
    /// <summary>
    /// Info bar manager
    /// </summary>
    public interface IInfoBarManager
    {
        /// <summary>
        /// Attached to an existing tool window
        /// </summary>
        /// <param name="toolwindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="buttonText">The button text</param>
        /// <param name="imageMoniker">Image</param>
        /// <returns><see cref="IInfoBar"/></returns>
        IInfoBar AttachInfoBar(Guid toolwindowGuid, string message, string buttonText, ImageMoniker imageMoniker);

        /// <summary>
        /// Detaches an <see cref="IInfoBar"/> from its tool window
        /// </summary>
        /// <param name="currentInfoBar">Instance of <see cref="IInfoBar"/> created by <see cref="AttachInfoBar(Guid, string, string)"/></param>
        void DetachInfoBar(IInfoBar currentInfoBar);
    }
}
