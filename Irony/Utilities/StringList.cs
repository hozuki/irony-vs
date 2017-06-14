#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System.Collections.Generic;

namespace Irony.Utilities {

    public sealed class StringList : List<string> {

        public StringList() {
        }

        public StringList(params string[] args)
            : base(args) {
        }

        public override string ToString() {
            return ToString(" ");
        }

        public string ToString(string separator) {
            return string.Join(separator, this);
        }

        // Used in sorting suffixes and prefixes; longer strings must come first in sort order
        public static int LongerFirst(string x, string y) {
            if (x == y) {
                return 0;
            }
            if (x == null || y == null) {
                return x == null ? 1 : -1;
            }
            return -x.Length.CompareTo(y.Length);
        }

    }

}
