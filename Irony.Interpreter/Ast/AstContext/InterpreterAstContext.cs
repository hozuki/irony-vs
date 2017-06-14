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

    //Extension of AstContext
    public sealed class InterpreterAstContext : AstContext {

        public InterpreterAstContext(LanguageData language)
            : this(language, null) {
        }

        public InterpreterAstContext(LanguageData language, OperatorHandler operatorHandler)
            : base(language) {
            OperatorHandler = operatorHandler ?? new OperatorHandler(language.Grammar.CaseSensitive);
            DefaultIdentifierNodeType = typeof(IdentifierNode);
            DefaultLiteralNodeType = typeof(LiteralValueNode);
            DefaultNodeType = null;
        }

        public OperatorHandler OperatorHandler { get; }

    }

}
