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

namespace Irony.Parsing {

    [Flags]
    public enum IdOptions : short {
        None = 0,
        AllowsEscapes = 0x01,
        CanStartWithEscape = 0x03,

        IsNotKeyword = 0x10,
        NameIncludesPrefix = 0x20,
    }


}
