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

namespace Irony.Parsing {

    public sealed class ParserStack : List<ParseTreeNode> {

        public ParserStack()
            : base(200) {
        }

        public void Push(ParseTreeNode node) {
            Add(node);
        }

        public void Push(ParseTreeNode nodeInfo, ParserState state) {
            nodeInfo.State = state;
            Push(nodeInfo);
        }

        public ParseTreeNode Pop() {
            if (Count < 0) {
                return null;
            }
            var item = this[Count - 1];
            RemoveAt(Count - 1);
            return item;
        }

        public void Pop(int count) {
            if (count <= 0) {
                return;
            }
            var origIndex = Count - count;
            var index = origIndex;
            if (index < 0) {
                index = 0;
            }
            if (origIndex != index) {
                count -= origIndex - index;
            }
            RemoveRange(index, count);
        }

        public void PopUntil(int finalCount) {
            if (finalCount < Count) {
                Pop(Count - finalCount);
            }
        }

        public ParseTreeNode Top => Count == 0 ? null : this[Count - 1];

    }

}
