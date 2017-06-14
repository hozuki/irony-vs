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

using System;
using System.Collections.Generic;

namespace Irony.Utilities {

    public sealed class StringSet : HashSet<string> {

        public StringSet() {
        }

        public StringSet(StringComparer comparer)
            : base(comparer) {
        }

        public override string ToString() {
            return ToString(" ");
        }

        public void AddRange(params string[] items) {
            UnionWith(items);
        }

        public string ToString(string separator) {
            return string.Join(separator, this);
        }

    }

}
