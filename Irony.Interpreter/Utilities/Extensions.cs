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

namespace Irony.Interpreter.Utilities {

    internal static class InterpreterEnumExtensions {

        public static bool IsSet(this BindingRequestFlags enumValue, BindingRequestFlags flag) {
            return (enumValue & flag) != 0;
        }

        public static bool IsSet(this AstNodeFlags enumValue, AstNodeFlags flag) {
            return (enumValue & flag) != 0;
        }

    }

}
