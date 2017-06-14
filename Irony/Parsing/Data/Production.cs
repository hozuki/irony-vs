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

using System.Text;

namespace Irony.Parsing {

    public sealed class Production {

        public Production(NonTerminal lvalue) {
            LValue = lvalue;
        }

        public ProductionFlags Flags { get; set; }

        // left-side element
        public NonTerminal LValue { get; }

        // the right-side elements sequence
        public BnfTermList RValues { get; } = new BnfTermList();

        internal LR0ItemList LR0Items { get; } = new LR0ItemList();        //LR0 items based on this production 

        public string ToStringQuoted() {
            return "'" + ToString() + "'";
        }

        public override string ToString() {
            //no dot
            return ProductionToString(this, -1);
        }

        public static string ProductionToString(Production production, int dotPosition) {
            //dot in the middle of the line
            const char dotChar = '\u00B7';
            var builder = new StringBuilder();

            builder.Append(production.LValue.Name);
            builder.Append(" -> ");
            for (var i = 0; i < production.RValues.Count; i++) {
                if (i == dotPosition) {
                    builder.Append(dotChar);
                }
                builder.Append(production.RValues[i].Name);
                builder.Append(" ");
            }
            if (dotPosition == production.RValues.Count) {
                builder.Append(dotChar);
            }

            return builder.ToString();
        }

    }

}
