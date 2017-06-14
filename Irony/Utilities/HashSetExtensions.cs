using System.Collections.Generic;

namespace Irony.Utilities {
    public static class HashSetExtensions {

        public static T[] ToArray<T>(this HashSet<T> set) {
            var count = set.Count;
            var array = new T[count];
            set.CopyTo(array);
            return array;
        }

    }
}
