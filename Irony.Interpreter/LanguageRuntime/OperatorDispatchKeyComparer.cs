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

namespace Irony.Interpreter {

    // Note: I believe (guess) that a custom Comparer provided to a Dictionary is a bit more efficient 
    // than implementing IComparable on the key itself
    public sealed class OperatorDispatchKeyComparer : IEqualityComparer<OperatorDispatchKey> {

        public bool Equals(OperatorDispatchKey x, OperatorDispatchKey y) {
            return x.HashCode == y.HashCode && x.Op == y.Op && x.Arg1Type == y.Arg1Type && x.Arg2Type == y.Arg2Type;
        }

        public int GetHashCode(OperatorDispatchKey obj) {
            return obj.HashCode;
        }

    }

}
