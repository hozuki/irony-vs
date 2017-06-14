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

namespace Irony.Parsing.ParserActions {

    public sealed class ShiftParserAction : ParserAction {

        public ShiftParserAction(LRItem item)
            : this(item.Core.Current, item.ShiftedItem.State) {
        }

        public ShiftParserAction(BnfTerm term, ParserState newState) {
            if (term == null) {
                throw new ArgumentNullException(nameof(term));
            }
            NewState = newState ?? throw new ArgumentNullException(nameof(newState), "ParserShiftAction: newState may not be null. term: " + term.ToString());
            Term = term;
        }

        public BnfTerm Term { get; }

        public ParserState NewState { get; }

        public override void Execute(ParsingContext context) {
            var currInput = context.CurrentParserInput;
            currInput.Term.OnShifting(context.SharedParsingEventArgs);
            context.ParserStack.Push(currInput, NewState);
            context.CurrentParserState = NewState;
            context.CurrentParserInput = null;
        }

        public override string ToString() {
            return string.Format(Resources.LabelActionShift, NewState.Name);
        }

    }

}
