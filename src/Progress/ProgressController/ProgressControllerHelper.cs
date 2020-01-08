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
using System.Globalization;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Helper class
    /// </summary>
    public static class ProgressControllerHelper
    {
        #region Public
        /// <summary>
        /// Indeterminate double representation
        /// </summary>
        public static readonly double Indeterminate = double.NaN;

        /// <summary>
        /// Returns whether the specified values is indeterminate
        /// </summary>
        /// <param name="progress">The progress for which to check</param>
        /// <returns>Whether considered to be indeterminate</returns>
        public static bool IsIndeterminate(double progress)
        {
            return double.IsNaN(progress);
        }

        /// <summary>
        /// Returns whether the state is considered to be final
        /// </summary>
        /// <param name="state">The state for which to check</param>
        /// <returns>Whether considered to be a final state</returns>
        public static bool IsFinalState(StepExecutionState state)
        {
            return state == StepExecutionState.Cancelled || state == StepExecutionState.Failed || state == StepExecutionState.Succeeded;
        }
        #endregion

        #region Non-public

        /// <summary>
        /// Creates a string in the specified format. The format string should have only one placeholder.
        /// </summary>
        /// <param name="ex">The exception to use in the format placeholder</param>
        /// <param name="messageErrorFormat">The format to use</param>
        /// <param name="logWholeMessage">Whether to use log the whole exception or just the message</param>
        /// <returns>The formatted string</returns>
        internal static string FormatErrorMessage(Exception ex, string messageErrorFormat, bool logWholeMessage)
        {
            return string.Format(CultureInfo.CurrentCulture, messageErrorFormat, logWholeMessage ? ex.ToString() : ex.Message);
        }
        #endregion
    }
}
