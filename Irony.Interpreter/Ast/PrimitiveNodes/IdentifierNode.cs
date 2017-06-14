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

    public sealed class IdentifierNode : AstNode {

        public string Symbol { get; private set; }

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            Symbol = treeNode.Token.ValueString;
            AsString = Symbol;
        }

        //Executed only once, on the first call
        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            _accessor = thread.Bind(Symbol, BindingRequestFlags.Read);
            Evaluate = _accessor.GetValueRef; // Optimization - directly set method ref to accessor's method. EvaluateReader;
            var result = Evaluate(thread);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void DoSetValue(ScriptThread thread, object value) {
            thread.CurrentNode = this;  //standard prolog
            if (_accessor == null) {
                _accessor = thread.Bind(Symbol, BindingRequestFlags.Write | BindingRequestFlags.ExistingOrNew);
            }
            _accessor.SetValueRef(thread, value);
            thread.CurrentNode = Parent;  //standard epilog
        }

        private Binding _accessor;

    }

}
