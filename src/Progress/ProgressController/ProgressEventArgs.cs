//-----------------------------------------------------------------------
// <copyright file="ProgressEventArgs.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Base class for <see cref="EventArgs"/>
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        // The base class has debug only verification code that can be used to verify serialization
        // of event raising and handling
        private int handled = 0;

        internal void Handled()
        {
            Interlocked.Increment(ref this.handled);
        }

        internal void CheckHandled()
        {
            Debug.WriteLine("The event arguments {0} were handled by {1} handlers", this.GetType().FullName, this.handled);
            Debug.Assert(this.handled > 0, "Caught unhandled event which was supposed to be handled", "Arguments type: {0}", this.GetType().FullName);
        }
    }
}
