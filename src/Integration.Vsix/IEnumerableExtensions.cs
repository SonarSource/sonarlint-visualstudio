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
        public static bool AllEqual<T>(this IEnumerable<T> values)
        {
            return IEnumerableExtensions.AllEqual(values, EqualityComparer<T>.Default);
        }

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

            return values.Distinct(comparer).Count() == 1;
        }
    }
}
