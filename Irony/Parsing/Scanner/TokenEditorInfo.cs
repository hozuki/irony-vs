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

namespace Irony.Parsing {

    // Helper classes for information used by syntax highlighters and editors
    // TokenColor, TokenTriggers and TokenType are copied from the Visual studio integration assemblies. 
    //  Each terminal/token would have its TokenEditorInfo that can be used either by VS integration package 
    //   or any editor for syntax highligting.
    public sealed class TokenEditorInfo {

        public TokenEditorInfo(TokenType type, TokenColor color, TokenTriggers triggers) {
            Type = type;
            Color = color;
            Triggers = triggers;
        }

        public TokenType Type { get; }

        public TokenColor Color { get; }

        public TokenTriggers Triggers { get; }

    }

}
