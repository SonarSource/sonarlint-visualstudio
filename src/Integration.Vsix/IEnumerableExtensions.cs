using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal static class IEnumerableExtensions
    {
        public static bool AllEqual<T>(this IEnumerable<T> values)
        {
            return values.AllEqual(EqualityComparer<T>.Default);
        }

        public static bool AllEqual<T>(this IEnumerable<T> values, IEqualityComparer<T> comparer)
        {
            return values.Distinct(comparer).Count() == 1;
        }
    }
}
