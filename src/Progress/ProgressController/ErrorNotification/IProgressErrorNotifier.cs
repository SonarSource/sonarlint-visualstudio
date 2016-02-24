//-----------------------------------------------------------------------
// <copyright file="IProgressErrorNotifier.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Progress.Controller.ErrorNotification
{
    /// <summary>
    /// Error notifier that is used by the <see cref="IProgressController"/>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Notifier", Justification = "False positive")]
    public interface IProgressErrorNotifier
    {
        /// <summary>
        /// Notifies that an error occurred
        /// </summary>
        /// <param name="ex">The error to notify</param>
        void Notify(Exception ex);
    }
}
