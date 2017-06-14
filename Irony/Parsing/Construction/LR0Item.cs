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

    public sealed class LR0Item {

        public LR0Item(int id, Production production, int position, GrammarHintList hints) {
            ID = id;
            Production = production;
            Position = position;
            Current = (Position < Production.RValues.Count) ? Production.RValues[Position] : null;
            if (hints != null) {
                Hints.AddRange(hints);
            }
            _hashCode = ID.ToString().GetHashCode();
        }

        public Production Production { get; }

        public int Position { get; }

        public BnfTerm Current { get; }

        public bool TailIsNullable { get; internal set; }

        public GrammarHintList Hints { get; } = new GrammarHintList();

        public LR0Item ShiftedItem => Position >= Production.LR0Items.Count - 1 ? null : Production.LR0Items[Position + 1];

        public bool IsKernel => Position > 0;

        public bool IsInitial => Position == 0;

        public bool IsFinal => Position == Production.RValues.Count;

        public override string ToString() {
            return Production.ProductionToString(Production, Position);
        }

        public override int GetHashCode() {
            return _hashCode;
        }

        //automatically generated IDs - used for building keys for lists of kernel LR0Items
        // which in turn are used to quickly lookup parser states in hash
        internal int ID { get; }

        private readonly int _hashCode;

    }

}
