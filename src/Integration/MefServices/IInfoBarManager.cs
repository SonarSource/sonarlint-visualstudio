/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using Microsoft.VisualStudio.Imaging.Interop;

namespace SonarLint.VisualStudio.Integration.InfoBar
{
    /// <summary>
    /// Info bar manager
    /// </summary>
    public interface IInfoBarManager
    {
        /// <summary>
        /// Attach an info bar with just a message to an existing tool window
        /// </summary>
        /// <param name="toolwindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="imageMoniker">Image</param>
        /// <returns><see cref="IInfoBar"/></returns>
        IInfoBar AttachInfoBar(Guid toolwindowGuid, string message, ImageMoniker imageMoniker);

        /// <summary>
        /// Attach an info bar with a message and a button to an existing tool window
        /// </summary>
        /// <param name="toolwindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="buttonText">The button text</param>
        /// <param name="imageMoniker">Image</param>
        /// <returns><see cref="IInfoBar"/></returns>
        IInfoBar AttachInfoBarWithButton(Guid toolwindowGuid, string message, string buttonText, ImageMoniker imageMoniker);

        /// <summary>
        /// Detaches an <see cref="IInfoBar"/> from its tool window
        /// </summary>
        /// <param name="currentInfoBar">Instance of <see cref="IInfoBar"/> created by <see cref="AttachInfoBar(Guid, string, string)"/></param>
        void DetachInfoBar(IInfoBar currentInfoBar);
    }
}
