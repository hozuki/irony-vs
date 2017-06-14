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
    public enum NodeUseType {
        Unknown,
        Name, //identifier used as a Name container - system would not use it's Evaluate method directly
        CallTarget,
        ValueRead,
        ValueWrite,
        ValueReadWrite,
        Parameter,
        Keyword,
        SpecialSymbol,
    }

}
