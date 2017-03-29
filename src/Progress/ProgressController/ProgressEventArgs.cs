/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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
