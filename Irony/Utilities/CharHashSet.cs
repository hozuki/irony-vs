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

    // CharHashSet: adding Hash to the name to avoid confusion with System.Runtime.Interoperability.CharSet
    // Adding case sensitivity
    public sealed class CharHashSet : HashSet<char> {

        public CharHashSet()
            : this(true) {
        }

        public CharHashSet(bool caseSensitive) {
            _caseSensitive = caseSensitive;
        }

        public new void Add(char ch) {
            if (_caseSensitive) {
                base.Add(ch);
            } else {
                base.Add(char.ToLowerInvariant(ch));
                base.Add(char.ToUpperInvariant(ch));
            }
        }

        private readonly bool _caseSensitive;

    }

}
