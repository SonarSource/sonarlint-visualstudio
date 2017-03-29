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
