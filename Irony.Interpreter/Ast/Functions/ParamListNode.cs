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

    public sealed class ParamListNode : AstNode {

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            foreach (var child in treeNode.ChildNodes) {
                AddChild(NodeUseType.Parameter, "param", child);
            }
            AsString = "param_list[" + ChildNodes.Count + "]";
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            // Is called once, at first evaluation of FunctionDefNode
            // Creates parameter slots
            foreach (var child in ChildNodes) {
                if (child is IdentifierNode idNode) {
                    thread.CurrentScope.Info.AddSlot(idNode.Symbol, SlotType.Parameter);
                }
            }
            Evaluate = EvaluateAfter;
            thread.CurrentNode = Parent; //standard epilog
            return null;
        }

        // TODO: implement handling list/dict parameter tails (Scheme, Python, etc)
        private object EvaluateAfter(ScriptThread thread) {
            return null;
        }

    }

}
