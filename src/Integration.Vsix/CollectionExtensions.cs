using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal static class CollectionExtensions
    {
        public static bool AllEqual<T>(this ICollection<T> values)
        {
            return values.AllEqual(EqualityComparer<T>.Default);
        }

        public static bool AllEqual<T>(this ICollection<T> values, IEqualityComparer<T> comparer)
        {
            return values.Distinct(comparer).Count() == 1;
        }
    }
}
