/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Static helper class to assert exception throwing behavior in delegates.
    /// </summary>
    public static class Exceptions
    {
        /// <summary>
        /// Executes the action. Returns if action throws expected exception.
        /// Raises Assert failure otherwise.
        /// </summary>
        /// <typeparam name="TException">Expected exception</typeparam>
        /// <param name="action">Action to run</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Using inference is ok here as we've constrained the type to be an Exception.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Necessary to turn it into a Test.Assert.")]
        public static void Expect<TException>(Action action) where TException : Exception
        {
            Expect<TException>((Action<TException>)null, action);
        }

        /// <summary>
        /// Executes the action. Returns if action throws expected exception.
        /// Raises Assert failure otherwise.
        /// Also asserts that the exception contains the expected error message.
        /// </summary>
        /// <typeparam name="TException">Expected exception</typeparam>
        /// <param name="expectedMessage">The expected error message. Can be null.</param>
        /// <param name="action">Action to run</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Using inference is ok here as we've constrained the type to be an Exception.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Necessary to turn it into a Test.Assert.")]
        public static void Expect<TException>(string expectedMessage, Action action) where TException : Exception
        {
            Action<Exception> checkMessage = e => e.Message.Should().Be(expectedMessage, "Unexpected error message");

            Expect<TException>(checkMessage, action);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Using inference is ok here as we've constrained the type to be an Exception.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Necessary to turn it into a Test.Assert.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Convenience")]
        public static void Expect<TException>(Action<TException> additionalChecks, Action action, bool checkDerived = false) where TException : Exception
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            try
            {
                action();
                FluentAssertions.Execution.Execute.Assertion.FailWith("Expected exception " + typeof(TException).FullName);
            }
            catch (TException ex)
            {
                if (!checkDerived && (ex.GetType() != typeof(TException)))
                {
                    FluentAssertions.Execution.Execute.Assertion.FailWith("Expected exception " + typeof(TException).FullName + " but got " + ex.GetType().FullName + "\n" + ex.ToString());
                }

                // Perform any additional checks that were supplied.
                if (additionalChecks != null)
                {
                    additionalChecks(ex);
                }
            }

        }
    }
}
