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

    //An object representing inter-state transitions. Defines Includes, IncludedBy that are used for efficient lookahead computation 
    public sealed class Transition {

        public Transition(ParserState fromState, NonTerminal overNonTerminal) {
            FromState = fromState;
            OverNonTerminal = overNonTerminal;
            ToState = FromState.BuilderData.GetNextState(overNonTerminal);
            _hashCode = unchecked(FromState.GetHashCode() - overNonTerminal.GetHashCode());
            FromState.BuilderData.Transitions.Add(overNonTerminal, this);
            Items = FromState.BuilderData.ShiftItems.SelectByCurrent(overNonTerminal);
            foreach (var item in Items) {
                item.Transition = this;
            }
        }

        public ParserState FromState { get; }

        public ParserState ToState { get; }

        public NonTerminal OverNonTerminal { get; }

        public LRItemSet Items { get; }

        public TransitionSet Includes { get; } = new TransitionSet();

        public TransitionSet IncludedBy { get; } = new TransitionSet();

        public void Include(Transition other) {
            if (other == this) {
                return;
            }
            if (!TryInclude(other)) {
                return;
            }
            //include children
            foreach (var child in other.Includes) {
                TryInclude(child);
            }
        }

        private bool TryInclude(Transition other) {
            if (!Includes.Add(other)) {
                return false;
            }

            other.IncludedBy.Add(this);
            //propagate "up"
            foreach (var incBy in IncludedBy) {
                incBy.TryInclude(other);
            }
            return true;
        }

        public override string ToString() {
            return $"{FromState.Name} -> (over {OverNonTerminal.Name}) -> {ToState.Name}";
        }

        public override int GetHashCode() {
            return _hashCode;
        }

        private readonly int _hashCode;

    }

}
