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

namespace Irony.Parsing.ParserActions {

    /// <summary>
    /// Reduces non-terminal marked as Transient by MarkTransient method. 
    /// </summary>
    public sealed class ReduceTransientParserAction : ReduceParserAction {

        public ReduceTransientParserAction(Production production)
            : base(production) {
        }

        protected override ParseTreeNode GetResultNode(ParsingContext context) {
            var topIndex = context.ParserStack.Count - 1;
            var childCount = Production.RValues.Count;
            for (var i = 0; i < childCount; i++) {
                var child = context.ParserStack[topIndex - i];
                if (child.IsPunctuationOrEmptyTransient) {
                    continue;
                }
                return child;
            }
            //Otherwise return an empty transient node; if it is part of the list, the list will skip it
            var span = context.ComputeStackRangeSpan(childCount);
            return new ParseTreeNode(Production.LValue, span);
        }

    }

}
