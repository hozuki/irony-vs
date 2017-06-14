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
    public enum RegexTermOptions {
        None = 0,
        AllowLetterAfter = 0x01, //if not set (default) then any following letter (after legal switches) is reported as invalid switch
        CreateRegExObject = 0x02,  //if set, token.Value contains Regex object; otherwise, it contains a pattern (string)
        UniqueSwitches = 0x04,    //require unique switches

        Default = CreateRegExObject | UniqueSwitches,
    }

}
