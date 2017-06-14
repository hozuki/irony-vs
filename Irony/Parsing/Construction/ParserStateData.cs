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
using System.Linq;
using Irony.Parsing.ParserActions;
using Irony.Utilities;

namespace Irony.Parsing {

    public sealed class ParserStateData {

        //used for creating canonical states from core set
        internal ParserStateData(ParserState state, LR0ItemSet kernelCores) {
            _state = state;
            foreach (var core in kernelCores) {
                AddItem(core);
            }
            IsInadequate = ReduceItems.Count > 1 || ReduceItems.Count == 1 && ShiftItems.Count > 0;
        }

        public LRItemSet AllItems { get; } = new LRItemSet();

        public LRItemSet ShiftItems { get; } = new LRItemSet();

        public LRItemSet ReduceItems { get; } = new LRItemSet();

        public LRItemSet InitialItems { get; } = new LRItemSet();

        public BnfTermSet ShiftTerms { get; } = new BnfTermSet();

        public TerminalSet ShiftTerminals { get; } = new TerminalSet();

        public TerminalSet Conflicts { get; } = new TerminalSet();

        public bool IsInadequate { get; }

        public void AddItem(LR0Item core) {
            //Check if a core had been already added. If yes, simply return
            if (!_allCores.Add(core)) {
                return;
            }
            //Create new item, add it to AllItems, InitialItems, ReduceItems or ShiftItems
            var item = new LRItem(_state, core);
            AllItems.Add(item);

            if (item.Core.IsFinal) {
                ReduceItems.Add(item);
            } else {
                ShiftItems.Add(item);
            }

            if (item.Core.IsInitial) {
                InitialItems.Add(item);
            }

            if (core.IsFinal) {
                return;
            }

            //Add current term to ShiftTerms
            if (!ShiftTerms.Add(core.Current)) {
                return;
            }

            if (core.Current is Terminal) {
                ShiftTerminals.Add(core.Current as Terminal);
            }

            //If current term (core.Current) is a new non-terminal, expand it
            var currNt = core.Current as NonTerminal;
            if (currNt == null) {
                return;
            }

            foreach (var prod in currNt.Productions) {
                AddItem(prod.LR0Items[0]);
            }
        }

        public TransitionTable Transitions => _transitions ?? (_transitions = new TransitionTable());

        //A set of states reachable through shifts over nullable non-terminals. Computed on demand
        public ParserStateSet ReadStateSet {
            get {
                if (_readStateSet != null) {
                    return _readStateSet;
                }

                var readStateSet = new ParserStateSet();
                foreach (var shiftTerm in _state.BuilderData.ShiftTerms) {
                    if (!shiftTerm.Flags.IsSet(TermFlags.IsNullable)) {
                        continue;
                    }
                    var shift = _state.Actions[shiftTerm] as ShiftParserAction;
                    if (shift == null) {
                        throw new NullReferenceException();
                    }
                    var targetState = shift.NewState;
                    readStateSet.Add(targetState);
                    readStateSet.UnionWith(targetState.BuilderData.ReadStateSet); //we shouldn't get into loop here, the chain of reads is finite
                }

                _readStateSet = readStateSet;
                return readStateSet;
            }
        }

        public ParserState GetNextState(BnfTerm shiftTerm) {
            var shift = ShiftItems.FirstOrDefault(item => item.Core.Current == shiftTerm);
            return shift?.ShiftedItem.State;
        }

        public TerminalSet GetShiftReduceConflicts() {
            var result = new TerminalSet();
            result.UnionWith(Conflicts);
            result.IntersectWith(ShiftTerminals);
            return result;
        }

        public TerminalSet GetReduceReduceConflicts() {
            var result = new TerminalSet();
            result.UnionWith(Conflicts);
            result.ExceptWith(ShiftTerminals);
            return result;
        }

        private readonly ParserState _state;
        private TransitionTable _transitions;
        private ParserStateSet _readStateSet;
        private readonly LR0ItemSet _allCores = new LR0ItemSet();

    }

}
