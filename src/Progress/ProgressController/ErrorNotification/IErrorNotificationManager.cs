//-----------------------------------------------------------------------
// <copyright file="IErrorNotificationManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Progress.Controller.ErrorNotification
{
    /// <summary>
    /// Manages the <see cref="IProgressErrorNotifier"/> which are used to notify about unhandled exceptions
    /// </summary>
    public interface IErrorNotificationManager : IProgressErrorNotifier
    {
        /// <summary>
        /// Adds a notifier
        /// </summary>
        /// <param name="notifier">Cannot be null</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Notifier", Justification = "False positive")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "notifier", Justification = "False positive")]
        void AddNotifier(IProgressErrorNotifier notifier);

        /// <summary>
        /// Removes a notifier
        /// </summary>
        /// <param name="notifier">Cannot be null</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Notifier", Justification = "False positive")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "notifier", Justification = "False positive")]
        void RemoveNotifier(IProgressErrorNotifier notifier);
    }
}
