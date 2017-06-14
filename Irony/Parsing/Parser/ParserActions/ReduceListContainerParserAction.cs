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

    //List container is an artificial non-terminal created by MakeStarRule method; the actual list is a direct child. 
    public sealed class ReduceListContainerParserAction : ReduceParserAction {

        public ReduceListContainerParserAction(Production production)
            : base(production) {
        }

        protected override ParseTreeNode GetResultNode(ParsingContext context) {
            var childCount = Production.RValues.Count;
            var firstChildIndex = context.ParserStack.Count - childCount;
            var span = context.ComputeStackRangeSpan(childCount);
            var newNode = new ParseTreeNode(Production.LValue, span);
            if (childCount > 0) { //if it is not empty production - might happen for MakeStarRule
                var listNode = context.ParserStack[firstChildIndex]; //get the transient list with all members - it is the first child node
                newNode.ChildNodes.AddRange(listNode.ChildNodes);    //copy all list members
            }
            return newNode;

        }
    }

}
