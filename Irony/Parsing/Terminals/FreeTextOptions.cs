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
    public enum FreeTextOptions {
        None = 0x0,
        ConsumeTerminator = 0x01, //move source pointer beyond terminator (so token "consumes" it from input), but don't include it in token text
        IncludeTerminator = 0x02, // include terminator into token text/value
        AllowEof = 0x04, // treat EOF as legitimate terminator
        AllowEmpty = 0x08,
    }

}
