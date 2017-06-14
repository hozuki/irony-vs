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
using Irony.Parsing.ParserActions;

namespace Irony.Parsing.SpecialActionHints {

    public sealed class PreferredActionHint : GrammarHint {

        public PreferredActionHint(PreferredActionType actionType) {
            _actionType = actionType;
        }

        public override void Apply(LanguageData language, LRItem owner) {
            var state = owner.State;
            var conflicts = state.BuilderData.Conflicts;
            if (conflicts.Count == 0) {
                return;
            }
            switch (_actionType) {
                case PreferredActionType.Shift:
                    var currTerm = owner.Core.Current as Terminal;
                    if (currTerm == null || !conflicts.Contains(currTerm)) {
                        //nothing to do
                        return;
                    }
                    //Current term for shift item (hint owner) is a conflict - resolve it with shift action
                    var shiftAction = new ShiftParserAction(owner);
                    state.Actions[currTerm] = shiftAction;
                    conflicts.Remove(currTerm);
                    return;
                case PreferredActionType.Reduce:
                    if (!owner.Core.IsFinal) {
                        //we take care of reduce items only here
                        return;
                    }
                    //we have a reduce item with "Reduce" hint. Check if any of lookaheads are in conflict
                    ReduceParserAction reduceAction = null;
                    foreach (var lkhead in owner.Lookaheads)
                        if (conflicts.Contains(lkhead)) {
                            if (reduceAction == null)
                                reduceAction = new ReduceParserAction(owner.Core.Production);
                            state.Actions[lkhead] = reduceAction;
                            conflicts.Remove(lkhead);
                        }
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private readonly PreferredActionType _actionType;

    }

}
