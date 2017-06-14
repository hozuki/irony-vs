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

namespace Irony.Interpreter {

    [Flags]
    public enum BindingRequestFlags {
        Read = 0x01,
        Write = 0x02,
        Invoke = 0x04,
        ExistingOrNew = 0x10,
        NewOnly = 0x20,  // for new variable, for ex, in JavaScript "var x..." - introduces x as new variable
    }

}
