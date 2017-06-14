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

using Irony.Parsing;

namespace Irony.Interpreter.Ast {

    //A stub to use when AST node was not created (type not specified on NonTerminal, or error on creation)
    // The purpose of the stub is to throw a meaningful message when interpreter tries to evaluate null node.
    public class NullNode : AstNode {

        internal NullNode(BnfTerm term) {
            Term = term;
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            thread.ThrowScriptError(Resources.ErrNullNodeEval, Term);
            return null; //never happens
        }

    }

}
