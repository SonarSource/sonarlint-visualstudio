//-----------------------------------------------------------------------
// <copyright file="AssertIgnoreScope.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Some of the tests cover exceptions in which we have asserts as well, we want to ignore those asserts during tests
    /// </summary>
    public class AssertIgnoreScope : IDisposable
    {
        public AssertIgnoreScope()
        {
            DefaultTraceListener listener = Debug.Listeners.OfType<DefaultTraceListener>().FirstOrDefault();
            if (listener != null)
            {
                listener.AssertUiEnabled = false;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DefaultTraceListener listener = Debug.Listeners.OfType<DefaultTraceListener>().FirstOrDefault();
                if (listener != null)
                {
                    listener.AssertUiEnabled = true;
                }
            }
        }
    }
}
