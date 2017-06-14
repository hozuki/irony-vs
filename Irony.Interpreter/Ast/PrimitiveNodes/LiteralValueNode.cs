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

    public class LiteralValueNode : AstNode {

        public object Value { get; private set; }

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            Value = treeNode.Token.Value;
            AsString = Value?.ToString() ?? "null";
            if (Value is string)
                AsString = "\"" + AsString + "\"";
        }

        protected override object DoEvaluate(ScriptThread thread) {
            return Value;
        }

        public override bool IsConstant => true;

    }

}
