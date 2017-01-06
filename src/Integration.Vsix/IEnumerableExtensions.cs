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
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// Checks if all values in the sequence are considered equal using
        /// the default <seealso cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the sequence elements</typeparam>
        /// <param name="values">Sequence to compare</param>
        /// <returns>True if the sequence is empty or if all elements are equal, false otherwise.</returns>
        public static bool AllEqual<T>(this IEnumerable<T> values)
        {
            return IEnumerableExtensions.AllEqual(values, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Checks if all values in the sequence are considered equal using
        /// the specified <seealso cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the sequence elements</typeparam>
        /// <param name="values">Sequence to compare</param>
        /// <param name="comparer">Comparer to use as part of equality checks</param>
        /// <returns>True if the sequence is empty or if all elements are equal, false otherwise.</returns>
        public static bool AllEqual<T>(this IEnumerable<T> values, IEqualityComparer<T> comparer)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return values.Distinct(comparer).Count() <= 1;
        }
    }
}
