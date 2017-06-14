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

using System.Diagnostics;

namespace Irony.Parsing {

    //Tokens are produced by scanner and fed to parser, optionally passing through Token filters in between. 
    public class Token {

        public Token(Terminal term, SourceLocation location, string text, object value) {
            SetTerminal(term);
            KeyTerm = term as KeyTerm;
            Location = location;
            Text = text;
            Value = value;
        }

        public Terminal Terminal { get; private set; }

        public KeyTerm KeyTerm { get; internal set; }

        public SourceLocation Location { get; }

        public string Text { get; }

        public object Value { get; internal set; }

        public string ValueString => Value?.ToString() ?? string.Empty;

        public object Details { get; internal set; }

        public TokenFlags Flags { get; internal set; }

        public TokenEditorInfo EditorInfo { get; internal set; }

        public void SetTerminal(Terminal terminal) {
            Terminal = terminal;
            EditorInfo = Terminal.EditorInfo;  //set to term's EditorInfo by default
        }

        public bool IsSet(TokenFlags flag) {
            return (Flags & flag) != 0;
        }

        public TokenCategory Category => Terminal.Category;

        public bool IsError => Category == TokenCategory.Error;

        public int Length => Text?.Length ?? 0;

        //matching opening/closing brace
        public Token OtherBrace { get; internal set; }

        //Scanner state after producing token 
        public short ScannerState { get; internal set; }

        [DebuggerStepThrough]
        public override string ToString() {
            return Terminal.TokenToString(this);
        }

    }

}
