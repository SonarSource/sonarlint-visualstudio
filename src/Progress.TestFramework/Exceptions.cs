/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using FluentAssertions;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Static helper class to assert exception throwing behavior in delegates.
    /// </summary>
    internal static class Exceptions
    {
        /// <summary>
        /// Executes the action. Returns if action throws expected exception.
        /// Raises Assert failure otherwise.
        /// </summary>
        /// <typeparam name="TException">Expected exception</typeparam>
        /// <param name="action">Action to run</param>
        /// <returns>The exception</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Using inference is ok here as we've constrained the type to be an Exception.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Necessary to turn it into a Test.Assert.")]
        [DebuggerStepThroughAttribute]
        public static TException Expect<TException>(Action action) where TException : Exception
        {
            return Expect<TException>((Action<TException>)null, action);
        }

        /// <summary>
        /// Executes the action. Returns if action throws expected exception.
        /// Raises Assert failure otherwise.
        /// Also asserts that the exception contains the expected error message.
        /// </summary>
        /// <typeparam name="TException">Expected exception</typeparam>
        /// <param name="expectedMessage">The expected error message. Can be null.</param>
        /// <param name="action">Action to run</param>
        /// <returns>The exception</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Using inference is ok here as we've constrained the type to be an Exception.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Necessary to turn it into a Test.Assert.")]
        [DebuggerStepThroughAttribute]
        public static TException Expect<TException>(string expectedMessage, Action action) where TException : Exception
        {
            Action<Exception> checkMessage = e => e.Message.Should().Be(expectedMessage, "Unexpected error message");

            return Expect<TException>(checkMessage, action);
        }

        /// <summary>
        /// Executes the action. Returns if action throws expected exception.
        /// Raises Assert failure otherwise.
        /// </summary>
        /// <typeparam name="TException">Expected exception</typeparam>
        /// <param name="additionalChecks">Any additional checks to be carried out once
        /// the exception has been thrown. Can be null.</param>
        /// <param name="action">Action to run</param>
        /// <param name="checkDerived">Whether to check exception is derived from the expected one (default false)</param>
        /// <returns>The exception</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Using inference is ok here as we've constrained the type to be an Exception.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Necessary to turn it into a Test.Assert.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Convenience")]
        [DebuggerStepThroughAttribute]
        public static TException Expect<TException>(Action<TException> additionalChecks, Action action, bool checkDerived = false) where TException : Exception
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            try
            {
                action();
            }
            catch (TException ex)
            {
                Debug.WriteLine("Exceptions.Expect: exception type = {0}", (object)ex.GetType().FullName);
                Debug.WriteLine("Exceptions.Expect: exception message = {0}", (object)ex.Message);

                if (!checkDerived && (ex.GetType() != typeof(TException)))
                {
                    FluentAssertions.Execution.Execute.Assertion.FailWith("Expected exception " + typeof(TException).FullName + " but got " + ex.GetType().FullName + "\n" + ex.ToString());
                }

                // Perform any additional checks that were supplied.
                if (additionalChecks != null)
                {
                    additionalChecks(ex);
                }

                return ex;
            }
            catch (Exception ex2)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Expected exception " + typeof(TException).FullName + " but got " + ex2.GetType().FullName + "\n" + ex2.ToString());
            }

            // No exception
            FluentAssertions.Execution.Execute.Assertion.FailWith("Expected exception " + typeof(TException).FullName);

            return null; // We'll never hit this, but it's required to stop the compiler complaining that not all code paths return a value.
        }
    }
}