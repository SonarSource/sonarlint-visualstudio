﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// VS-agnostic abstraction over common thread-related operations
    /// </summary>
    public interface IThreadHandling
    {
        /// <summary>
        /// Throws an exception if the call is being made on the UI thread
        /// </summary>
        void ThrowIfOnUIThread();

        /// <summary>
        /// Throws an exception if the call is not being made on the UI thread
        /// </summary>
        void ThrowIfNotOnUIThread();

        /// <summary>
        /// Checks call is being made on the UI thread or not
        /// </summary>
        /// <returns>True if on the UI thread, otherwise false</returns>
        bool CheckAccess();

        /// <summary>
        /// Executes the operation asynchronously on the main thread.
        /// If the caller is on the main thread already then the operation is executed directly.
        /// If the caller is not on the main thread then the method will switch to the main thread,
        /// then resume on the caller's thread when then the operation completes.
        /// </summary>
        Task RunOnUIThread(Action op);
    }
}
