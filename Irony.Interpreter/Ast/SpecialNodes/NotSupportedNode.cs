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

    //A substitute node to use on constructs that are not yet supported by language implementation.
    // The script would compile Ok but on attempt to evaluate the node would throw a runtime exception
    public sealed class NotSupportedNode : AstNode {
        
        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            _name = treeNode.Term.ToString();
            AsString = _name + " (not supported)";
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            thread.ThrowScriptError(Resources.ErrConstructNotSupported, _name);
            return null; //never happens
        }

        private string _name;

    }

}
