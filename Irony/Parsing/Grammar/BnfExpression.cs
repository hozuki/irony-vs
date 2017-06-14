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

namespace Irony.Parsing {

    public sealed class BnfExpression : BnfTerm {

        public BnfExpression(BnfTerm element)
            : this() {
            Data[0].Add(element);
        }

        public BnfExpression()
            : base(null) {
            Data = new BnfExpressionData {
                new BnfTermList()
            };
        }

        public override string ToString() {
            return Data.ToString();
        }

        internal BnfExpressionData Data { get; }

        #region Implicit cast operators
        public static implicit operator BnfExpression(string symbol) {
            return new BnfExpression(Grammar.CurrentGrammar.ToTerm(symbol));
        }

        // It seems better to define one method instead of the following two, with parameter of type BnfTerm -
        // but that's not possible - it would be a conversion from base type of BnfExpression itself, which
        // is not allowed in C#.
        public static implicit operator BnfExpression(Terminal term) {
            return new BnfExpression(term);
        }

        public static implicit operator BnfExpression(NonTerminal nonTerminal) {
            return new BnfExpression(nonTerminal);
        }
        #endregion


    }

}
