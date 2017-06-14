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
    public enum StringOptions : short {

        None = 0,
        IsChar = 0x01,
        AllowsDoubledQuote = 0x02, //Convert doubled start/end symbol to a single symbol; for ex. in SQL, '' -> '
        AllowsLineBreak = 0x04,
        IsTemplate = 0x08, //Can include embedded expressions that should be evaluated on the fly; ex in Ruby: "hello #{name}"
        NoEscapes = 0x10,
        AllowsUEscapes = 0x20,
        AllowsXEscapes = 0x40,
        AllowsOctalEscapes = 0x80,
        AllowsAllEscapes = AllowsUEscapes | AllowsXEscapes | AllowsOctalEscapes,

    }

}
