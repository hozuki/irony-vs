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
using System.Linq.Expressions;

namespace Irony.Interpreter {

    /// <summary>
    /// The struct is used as a key for the dictionary of operator implementations. 
    /// Contains types of arguments for a method or operator implementation.
    /// </summary>
    public struct OperatorDispatchKey : IEquatable<OperatorDispatchKey> {

        //For binary operators
        public OperatorDispatchKey(ExpressionType op, Type arg1Type, Type arg2Type) {
            Op = op;
            Arg1Type = arg1Type;
            Arg2Type = arg2Type;
            var h0 = (int)Op;
            var h1 = Arg1Type.GetHashCode();
            var h2 = Arg2Type.GetHashCode();
            HashCode = unchecked(h0 << 8 ^ h1 << 4 ^ h2);
        }

        //For unary operators
        public OperatorDispatchKey(ExpressionType op, Type arg1Type) {
            Op = op;
            Arg1Type = arg1Type;
            Arg2Type = null;
            var h0 = (int)Op;
            var h1 = Arg1Type.GetHashCode();
            var h2 = 0;
            HashCode = unchecked(h0 << 8 ^ h1 << 4 ^ h2);
        }

        public static OperatorDispatchKeyComparer Comparer { get; } = new OperatorDispatchKeyComparer();

        public ExpressionType Op { get; }

        public Type Arg1Type { get; }

        public Type Arg2Type { get; }

        public int HashCode { get; }

        public bool Equals(OperatorDispatchKey other) {
            return Comparer.Equals(this, other);
        }

        public override int GetHashCode() {
            return HashCode;
        }

        public override string ToString() {
            return Op + "(" + Arg1Type + ", " + Arg2Type + ")";
        }

    }

}
