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
    public enum TermFlags {
        None = 0,
        IsOperator = 0x01,
        IsOpenBrace = 0x02,
        IsCloseBrace = 0x04,
        IsBrace = IsOpenBrace | IsCloseBrace,
        IsLiteral = 0x08,

        IsConstant = 0x10,
        IsPunctuation = 0x20,
        IsDelimiter = 0x40,
        IsReservedWord = 0x080,
        IsMemberSelect = 0x100,
        InheritPrecedence = 0x200, // Signals that non-terminal must inherit precedence and assoc values from its children. 
        // Typically set for BinOp nonterminal (where BinOp.Rule = '+' | '-' | ...) 

        IsNonScanner = 0x01000,  // indicates that tokens for this terminal are NOT produced by scanner 
        IsNonGrammar = 0x02000,  // if set, parser would eliminate the token from the input stream; terms in Grammar.NonGrammarTerminals have this flag set
        IsTransient = 0x04000,  // Transient non-terminal - should be replaced by it's child in the AST tree.
        IsNotReported = 0x08000,  // Exclude from expected terminals list on syntax error

        //calculated flags
        IsNullable = 0x010000,
        IsVisible = 0x020000,
        IsKeyword = 0x040000,
        IsMultiline = 0x100000,
        //internal flags
        IsList = 0x200000,
        IsListContainer = 0x400000,
        //Indicates not to create AST node; mainly to suppress warning message on some special nodes that AST node type is not specified
        //Automatically set by MarkTransient method
        NoAstNode = 0x800000,
        //A flag to suppress automatic AST creation for child nodes in global AST construction. Will be used to supress full 
        // "compile" of method bodies in modules. The module might be large, but the running code might 
        // be actually using only a few methods or global members; so in this case it makes sense to "compile" only global/public
        // declarations, including method headers but not full bodies. The body will be compiled on the first call. 
        // This makes even more sense when processing module imports. 
        AstDelayChildren = 0x1000000,

    }

}
