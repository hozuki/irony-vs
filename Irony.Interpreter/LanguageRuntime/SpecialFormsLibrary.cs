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

    public static class SpecialFormsLibrary {

        public static object IIf(ScriptThread thread, AstNode[] childNodes) {
            var testValue = childNodes[0].Evaluate(thread);
            var result = thread.Runtime.IsTrue(testValue) ? childNodes[1].Evaluate(thread) : childNodes[2].Evaluate(thread);
            return result;
        }

    }

}
