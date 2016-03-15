//-----------------------------------------------------------------------
// <copyright file="IBindingOperation.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Three-step binding operation
    /// </summary>
    internal interface IBindingOperation
    {
        /// <summary>
        /// Initializes the initial state. Called on the foreground thread.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Prepares for binding. Called on the background thread.
        /// </summary>
        void Prepare(CancellationToken token);

        /// <summary>
        /// Binds. Called on the foreground thread.
        /// </summary>
        void Commit();
    }
}
