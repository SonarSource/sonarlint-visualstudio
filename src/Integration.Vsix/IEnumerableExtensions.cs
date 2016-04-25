//-----------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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

            return values.Distinct(comparer).Count() == 1 || !values.Any();
        }
    }
}
