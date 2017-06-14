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
using System.Linq.Expressions;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {

    public sealed class OperatorInfoDictionary : Dictionary<string, OperatorInfo> {

        public OperatorInfoDictionary(bool caseSensitive)
            : base(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase) {
        }

        public void Add(string symbol, ExpressionType expressionType, int precedence, Associativity associativity = Associativity.Left) {
            var info = new OperatorInfo {
                Symbol = symbol,
                ExpressionType = expressionType,
                Precedence = precedence,
                Associativity = associativity
            };
            this[symbol] = info;
        }
    }

}
