/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Core.InfoBar
{
    public interface IInfoBarManager
    {
        /// <summary>
        /// Attach an info bar with just a message to an existing tool window
        /// </summary>
        /// <param name="toolWindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="imageMoniker">Image</param>
        /// <returns><see cref="IInfoBar"/></returns>
        IInfoBar AttachInfoBar(Guid toolWindowGuid, string message, SonarLintImageMoniker imageMoniker);

        /// <summary>
        /// Attach an info bar with a message and a button to an existing tool window
        /// </summary>
        /// <remarks>
        /// The created info bar will have one button with a "button" style
        /// </remarks>
        /// <param name="toolWindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="buttonText">The button text</param>
        /// <param name="imageMoniker">Image</param>
        IInfoBar AttachInfoBarWithButton(Guid toolWindowGuid, string message, string buttonText, SonarLintImageMoniker imageMoniker);

        /// <summary>
        /// Attach an info bar with a message and multiple buttons to an existing tool window
        /// </summary>
        /// <remarks>
        /// The created info bar will have multiple buttons with a "hyperlink" style
        /// </remarks>
        IInfoBar AttachInfoBarWithButtons(Guid toolWindowGuid, string message, IEnumerable<string> buttonTexts, SonarLintImageMoniker imageMoniker);

        /// <summary>
        /// Attach an info bar with just a message to the main window
        /// </summary>
        /// <param name="toolWindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="imageMoniker">Image</param>
        /// <returns><see cref="IInfoBar"/></returns>
        IInfoBar AttachInfoBarMainWindow(string message, SonarLintImageMoniker imageMoniker);

        /// <summary>
        /// Attach an info bar with a message and a button to the main window
        /// </summary>
        /// <remarks>
        /// The created info bar will have one button with a "button" style
        /// </remarks>
        /// <param name="toolWindowGuid">Tool window Guid</param>
        /// <param name="message">Message to show on the info bar</param>
        /// <param name="buttonText">The button text</param>
        /// <param name="imageMoniker">Image</param>
        IInfoBar AttachInfoBarWithButtonMainWindow(string message, string buttonText, SonarLintImageMoniker imageMoniker);

        /// <summary>
        /// Attach an info bar with a message and multiple buttons to the main window
        /// </summary>
        /// <remarks>
        /// The created info bar will have multiple buttons with a "hyperlink" style
        /// </remarks>
        IInfoBar AttachInfoBarWithButtonsMainWindow(string message, IEnumerable<string> buttonTexts, SonarLintImageMoniker imageMoniker);

        /// <summary>
        /// Detaches an <see cref="IInfoBar"/> from its tool window
        /// </summary>
        /// <param name="currentInfoBar">Instance of <see cref="IInfoBar"/> created by <see cref="AttachInfoBar(Guid, string, SonarLintImageMoniker)"/></param>
        void DetachInfoBar(IInfoBar currentInfoBar);
    }
}
