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

using System.Collections.Generic;
using System.Linq;

namespace Irony.Parsing {

    public sealed class LRItemSet : HashSet<LRItem> {

        public LRItem FindByCore(LR0Item core) {
            return this.FirstOrDefault(item => item.Core == core);
        }

        public LRItemSet SelectByCurrent(BnfTerm current) {
            var result = new LRItemSet();
            foreach (var item in this) {
                if (item.Core.Current == current) {
                    result.Add(item);
                }
            }

            return result;
        }

        public LR0ItemSet GetShiftedCores() {
            var result = new LR0ItemSet();
            foreach (var item in this) {
                if (item.Core.ShiftedItem != null) {
                    result.Add(item.Core.ShiftedItem);
                }
            }

            return result;
        }

        public LRItemSet SelectByLookahead(Terminal lookahead) {
            var result = new LRItemSet();
            foreach (var item in this) {
                if (item.Lookaheads.Contains(lookahead)) {
                    result.Add(item);
                }
            }

            return result;
        }

    }//class

}
