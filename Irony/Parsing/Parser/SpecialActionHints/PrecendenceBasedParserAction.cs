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

using Irony.Parsing.ParserActions;

namespace Irony.Parsing.SpecialActionHints {

    public sealed class PrecedenceBasedParserAction : ConditionalParserAction {

        public PrecedenceBasedParserAction(BnfTerm shiftTerm, ParserState newShiftState, Production reduceProduction) {
            _reduceAction = new ReduceParserAction(reduceProduction);
            var reduceEntry = new ConditionalEntry(CheckMustReduce, _reduceAction, "(Precedence comparison)");
            ConditionalEntries.Add(reduceEntry);
            DefaultAction = _shiftAction = new ShiftParserAction(shiftTerm, newShiftState);
        }

        private bool CheckMustReduce(ParsingContext context) {
            var input = context.CurrentParserInput;
            var stackCount = context.ParserStack.Count;
            var prodLength = _reduceAction.Production.RValues.Count;
            for (var i = 1; i <= prodLength; i++) {
                var prevNode = context.ParserStack[stackCount - i];
                if (prevNode == null) {
                    continue;
                }
                if (prevNode.Precedence == BnfTerm.NoPrecedence) {
                    continue;
                }
                //if previous operator has the same precedence then use associativity
                if (prevNode.Precedence == input.Precedence) {
                    //if true then Reduce
                    return input.Associativity == Associativity.Left;
                }
                //if true then Reduce
                return prevNode.Precedence > input.Precedence;
            }
            //If no operators found on the stack, do shift
            return false;
        }

        public override string ToString() {
            return string.Format(Resources.LabelActionOp, _shiftAction.NewState.Name, _reduceAction.Production.ToStringQuoted());
        }

        private readonly ShiftParserAction _shiftAction;
        private readonly ReduceParserAction _reduceAction;

    }

}
