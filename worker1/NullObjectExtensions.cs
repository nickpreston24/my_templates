using System.Diagnostics.CodeAnalysis;

namespace worker1;

public static class NullObjectExtensions
{
    public static T FirstOrNullObject<T>(this IEnumerable<T> collection, [DisallowNull] T fallback)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        return collection.FirstOrDefault() ?? fallback;
    }
}