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

namespace Irony.Interpreter.Ast {

    public sealed class Closure : ICallTarget {

        internal Closure(Scope parentScope, LambdaNode targetNode) {
            ParentScope = parentScope;
            Lambda = targetNode;
        }

        //The scope that created closure; is used to find Parents (enclosing scopes) 
        public Scope ParentScope { get; }

        public LambdaNode Lambda { get; }

        public object Call(ScriptThread thread, object[] parameters) {
            return Lambda.Call(ParentScope, thread, parameters);
        }

        public override string ToString() {
            return Lambda.ToString(); //returns nice string like "<function add>"
        }

    }

}
