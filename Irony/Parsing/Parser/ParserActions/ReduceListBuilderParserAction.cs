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
    /// Reduces list created by MakePlusRule or MakeListRule methods. 
    /// </summary>
    public sealed class ReduceListBuilderParserAction : ReduceParserAction {

        public ReduceListBuilderParserAction(Production production)
            : base(production) {
        }

        protected override ParseTreeNode GetResultNode(ParsingContext context) {
            var childCount = Production.RValues.Count;
            var firstChildIndex = context.ParserStack.Count - childCount;
            var listNode = context.ParserStack[firstChildIndex]; //get the list already created - it is the first child node
            listNode.Span = context.ComputeStackRangeSpan(childCount);
            var listMember = context.ParserStack.Top; //next list member is the last child - at the top of the stack
            if (listMember.IsPunctuationOrEmptyTransient) {
                return listNode;
            }
            listNode.ChildNodes.Add(listMember);
            return listNode;
        }

    }

}
