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

namespace SonarLint.VisualStudio.Core
{
    public class DocumentClosedEventArgs : EventArgs
    {
        /// <summary>
        /// Full file path to the document being closed
        /// </summary>
        public string FullPath { get; }

        public DocumentClosedEventArgs(string fullPath)
        {
            FullPath = fullPath;
        }
    }

    /// <summary>
    /// Raises notifications about document events
    /// </summary>
    public interface IDocumentEvents
    {
        /// <summary>
        /// Notification that a document has closed
        /// </summary>
        event EventHandler<DocumentClosedEventArgs> DocumentClosed;
    }
}
