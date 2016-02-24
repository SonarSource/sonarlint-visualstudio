//-----------------------------------------------------------------------
// <copyright file="ProgressControllerHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
        /// <param name="ex">The expection to use in the format placeholder</param>
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
