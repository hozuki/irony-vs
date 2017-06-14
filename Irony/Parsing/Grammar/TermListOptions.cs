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

    //Used by Make-list-rule methods
    [Flags]
    public enum TermListOptions {
        None = 0,
        AllowEmpty = 0x01,
        AllowTrailingDelimiter = 0x02,

        // In some cases this hint would help to resolve the conflicts that come up when you have two lists separated by a nullable term.
        // This hint would resolve the conflict, telling the parser to include as many as possible elements in the first list, and the rest, 
        // if any, would go to the second list. By default, this flag is included in Star and Plus lists. 
        AddPreferShiftHint = 0x04,
        //Combinations - use these 
        PlusList = AddPreferShiftHint,
        StarList = AllowEmpty | AddPreferShiftHint,
    }

}
