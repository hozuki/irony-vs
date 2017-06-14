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

namespace Irony.Parsing {

    //BNF expressions are represented as OR-list of Plus-lists of BNF terms
    internal sealed class BnfExpressionData : List<BnfTermList> {

        public override string ToString() {
            try {
                var pipeArr = new string[Count];
                var i = 0;
                foreach (var seq in this) {
                    var seqArr = new string[seq.Count];
                    for (var j = 0; j < seq.Count; j++) {
                        seqArr[j] = seq[j].ToString();
                    }
                    pipeArr[i] = string.Join("+", seqArr);
                    ++i;
                }
                return string.Join("|", pipeArr);
            } catch (Exception e) {
                return "(error: " + e.Message + ")";
            }
        }

    }

}
