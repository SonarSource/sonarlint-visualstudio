/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Linq;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    internal interface IClientSuppressionSynchronizer
    {
        /// <summary>
        /// Notifies listeners that the set of local suppressions has changed
        /// </summary>
        event EventHandler<LocalSuppressionsChangedEventArgs> LocalSuppressionsChanged;

        /// <summary>
        /// Synchronizes server side issues with client side issues.
        /// </summary>
        void SynchronizeSuppressedIssues();
    }

    internal class LocalSuppressionsChangedEventArgs : EventArgs
    {
        public LocalSuppressionsChangedEventArgs(IEnumerable<string> changedFiles)
            => ChangedFiles = changedFiles?.ToArray() ?? Array.Empty<string>();

        /// <summary>
        /// The list of files affected by the issues that were updated
        /// </summary>
        public IEnumerable<string> ChangedFiles { get; }
    }
}
