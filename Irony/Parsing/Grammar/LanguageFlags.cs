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
    public enum LanguageFlags {
        None = 0,

        //Compilation options
        //Be careful - use this flag ONLY if you use NewLine terminal in grammar explicitly!
        // - it happens only in line-based languages like Basic.
        NewLineBeforeEof = 0x01,
        //Emit LineStart token
        EmitLineStartToken = 0x02,
        DisableScannerParserLink = 0x04, //in grammars that define TokenFilters (like Python) this flag should be set
        CreateAst = 0x08, //create AST nodes 

        //Runtime
        SupportsCommandLine = 0x0200,
        TailRecursive = 0x0400, //Tail-recursive language - Scheme is one example
        SupportsBigInt = 0x01000,
        SupportsComplex = 0x02000,
        SupportsRational = 0x04000,

        //Default value
        Default = None,
    }

}
