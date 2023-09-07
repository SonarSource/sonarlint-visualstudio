/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

#if DEBUG

// Disable issues in this debug-only file
#pragma warning disable VSTHRD104   // "Expose an async version of the method" - using the pattern from VS Cookbook
#pragma warning disable IDE0079     // "Remove unnecessary suppressiom"
#pragma warning disable S101        // "Rename class to match Pascal case naming rules"

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    // ******************************************************
    // **
    // **  Not be to used in shipping production code !
    // **
    // **  Only use for testing whether code is free-threaded
    // **  in local dev environments.
    // **
    // ******************************************************


    /* From the VS threading cookbook: https://github.com/microsoft/vs-threading/blob/main/doc/cookbook_vs.md
 
        "Important places to be thread-safe include:

        1. Code that runs in the MEF activation code paths (e.g. importing constructors, OnImportsSatisfied callbacks,
           and any code called therefrom).
        2. Code that may run on a background thread, especially when run within an async context.
        3. Static field initializers and static constructors."


        Instructions for use:
        ---------------------
        * Decide which code you want to test
        * Call the code to be tested using one of the helper methods in this class
        * Execute the code in VS
        
        If VS deadlocks the code is not free-threaded.

        From our point of view, cases (1) and (2) above are most relevant.

        Any code that needs the UI thread is by definition not free-threaded (e.g. to request a VS service)
        is by definition NOT free-threaded, so it will definitely deadlock when using these helper methods.

    */
    public static class DEBUG_ONLY_VsCookbookThreadingChecker
    {
        /// <summary>
        /// If this method is called on the UI thread it will check
        /// whether the operation _requires_ the UI thread.
        /// If the method is called from a background thread it will just execute the operation.
        /// </summary>
        public static void IfOnUIThreadCheckIfRequiresMainThread(Action operationToTest)
        {
            if (ThreadHelper.CheckAccess())
            {
                Debug.WriteLine("On UI thread: testing operation...");
                CheckIfRequiresMainThread(operationToTest);
            }
            else
            {
                Debug.WriteLine("Not on UI thread: skipping the check, just executing the operation...");
                operationToTest();
            }
        }

        /// <summary>
        /// If this method is called on the UI thread it will check
        /// whether the operation _requires_ the UI thread.
        /// If the method is called from a background thread it will just execute the operation.
        /// </summary>
        public static T IfOnUIThreadCheckIfRequiresMainThread<T>(Func<T> operationToTest)
        {
            if (ThreadHelper.CheckAccess())
            {
                Debug.WriteLine("On UI thread: testing operation...");
                return CheckIfRequiresMainThread(operationToTest);
            }

            Debug.WriteLine("Not on UI thread: skipping the check, just executing the operation...");
            return operationToTest();
        }

        /// <summary>
        /// Checks if the operation _requires_ the UI thread.
        /// Note: this method must be called from the UI thread, otherwise
        /// it will throw.
        /// </summary>
        public static void CheckIfRequiresMainThread(Action operationToTest)
        {
            ThreadHelper.ThrowIfNotOnUIThread(); // this test only catches issues when it blocks the UI thread.

            // Based on code from the VS threading cookbook
            // https://github.com/microsoft/vs-threading/blob/main/doc/cookbook_vs.md

            var jtf = ThreadHelper.JoinableTaskFactory;

            jtf.Run(async delegate
            {
                using (jtf.Context.SuppressRelevance())
                {
                    await Task.Run(delegate
                    {
                        operationToTest();
                    });
                }
            });
        }

        /// <summary>
        /// Checks if the operation _requires_ the UI thread.
        /// Note: this method must be called from the UI thread, otherwise
        /// it will throw.
        /// </summary>
        public static T CheckIfRequiresMainThread<T>(Func<T> operationToTest)
        {
            ThreadHelper.ThrowIfNotOnUIThread(); // this test only catches issues when it blocks the UI thread.

            // Based on code from the VS threading cookbook
            // https://github.com/microsoft/vs-threading/blob/main/doc/cookbook_vs.md
            
            var jtf = ThreadHelper.JoinableTaskFactory;

            jtf.Run(async delegate
            {
                using (jtf.Context.SuppressRelevance())
                {
                    await Task.Run(delegate
                    {
                        return operationToTest();
                    });
                }
            });

            return default;
        }
    }
}

#endif // DEBUG
