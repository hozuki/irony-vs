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
    public enum TokenType {
        Unknown = 0,
        Text = 0x1,
        Keyword = 0x2,
        Identifier = 0x4,
        String = 0x8,
        Literal = 0x10,
        Operator = 0x20,
        Delimiter = 0x40,
        WhiteSpace = 0x80,
        LineComment = 0x100,
        Comment = 0x200,
    }

}
