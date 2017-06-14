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
    public enum NumberOptions {
        None = 0,
        Default = None,

        AllowStartEndDot = 0x01,     //python : http://docs.python.org/ref/floating.html
        IntOnly = 0x02,
        NoDotAfterInt = 0x04,     //for use with IntOnly flag; essentially tells terminal to avoid matching integer if 
        // it is followed by dot (or exp symbol) - leave to another terminal that will handle float numbers
        AllowSign = 0x08,
        DisableQuickParse = 0x10,
        AllowLetterAfter = 0x20,      // allow number be followed by a letter or underscore; by default this flag is not set, so "3a" would not be 
        //  recognized as number followed by an identifier
        AllowUnderscore = 0x40,      // Ruby allows underscore inside number: 1_234

        //The following should be used with base-identifying prefixes
        Binary = 0x0100, //e.g. GNU GCC C Extension supports binary number literals
        Octal = 0x0200,
        Hex = 0x0400,
    }

}
