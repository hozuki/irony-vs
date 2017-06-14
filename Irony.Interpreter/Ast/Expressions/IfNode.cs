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

using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {

    public sealed class IfNode : AstNode {
        
        public AstNode Test { get; private set; }

        public AstNode IfTrue { get; private set; }

        public AstNode IfFalse { get; private set; }

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            Test = AddChild("Test", nodes[0]);
            IfTrue = AddChild("IfTrue", nodes[1]);
            if (nodes.Count > 2) {
                IfFalse = AddChild("IfFalse", nodes[2]);
            }
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            object result = null;
            var test = Test.Evaluate(thread);
            var isTrue = thread.Runtime.IsTrue(test);
            if (isTrue) {
                if (IfTrue != null) {
                    result = IfTrue.Evaluate(thread);
                }
            } else {
                if (IfFalse != null) {
                    result = IfFalse.Evaluate(thread);
                }
            }
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void SetIsTail() {
            base.SetIsTail();
            IfTrue?.SetIsTail();
            IfFalse?.SetIsTail();
        }

    }

}
