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

using Irony.Interpreter.Ast;

namespace Irony.Interpreter {

    //Binding request is a container for information about requested binding. Binding request goes from an Ast node to language runtime. 
    // For example, identifier node would request a binding for an identifier. 
    public sealed class BindingRequest {

        public BindingRequest(ScriptThread thread, AstNode fromNode, string symbol, BindingRequestFlags flags) {
            Thread = thread;
            FromNode = fromNode;
            FromModule = thread.App.DataMap.GetModule(fromNode.ModuleNode);
            Symbol = symbol;
            Flags = flags;
            FromScopeInfo = thread.CurrentScope.Info;
            IgnoreCase = !thread.Runtime.Language.Grammar.CaseSensitive;
        }

        public ScriptThread Thread { get; }

        public AstNode FromNode { get; }

        public ModuleInfo FromModule { get; }

        public BindingRequestFlags Flags { get; }

        public string Symbol { get; }

        public ScopeInfo FromScopeInfo { get; }

        public bool IgnoreCase { get; }

    }

}
