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

using System;

namespace Irony.Interpreter.Ast {

    [Flags]
    public enum AstNodeFlags {
        None = 0x0,
        IsTail = 0x01,     //the node is in tail position
        IsScope = 0x02,     //node defines scope for local variables
    }

}
