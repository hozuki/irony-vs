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

    public sealed class LRItem {

        public LRItem(ParserState state, LR0Item core) {
            State = state;
            Core = core;
            _hashCode = unchecked(state.GetHashCode() + core.GetHashCode());
        }

        public ParserState State { get; }

        public LR0Item Core { get; }

        //these properties are used in lookahead computations
        public LRItem ShiftedItem { get; internal set; }

        public Transition Transition { get; internal set; }

        //Lookahead info for reduce items
        public TransitionSet Lookbacks { get; } = new TransitionSet();

        public TerminalSet Lookaheads { get; } = new TerminalSet();

        public override string ToString() {
            return Core.ToString();
        }

        public override int GetHashCode() {
            return _hashCode;
        }

        public TerminalSet GetLookaheadsInConflict() {
            var lkhc = new TerminalSet();
            lkhc.UnionWith(Lookaheads);
            lkhc.IntersectWith(State.BuilderData.Conflicts);
            return lkhc;
        }

        private readonly int _hashCode;

    }

}
