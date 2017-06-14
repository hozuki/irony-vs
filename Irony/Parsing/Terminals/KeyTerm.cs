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
using System.Collections.Generic;
using System.Diagnostics;
using Irony.Utilities;

namespace Irony.Parsing {

    //Keyterm is a keyword or a special symbol used in grammar rules, for example: begin, end, while, =, *, etc.
    // So "key" comes from the Keyword. 
    public class KeyTerm : Terminal, IEquatable<KeyTerm> {

        public KeyTerm(string text, string name)
            : base(name) {
            Text = text;
            ErrorAlias = name;
            Flags |= TermFlags.NoAstNode;
        }

        public string Text { get; }

        //Normally false, meaning keywords (symbols in grammar consisting of letters) cannot be followed by a letter or digit
        public bool AllowAlphaAfterKeyword { get; set; } = false;

        #region overrides: TryMatch, Init, GetPrefixes(), ToString() 
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);

            #region comments about keyterms priority
            // Priority - determines the order in which multiple terminals try to match input for a given current char in the input.
            // For a given input char the scanner looks up the collection of terminals that may match this input symbol. It is the order
            // in this collection that is determined by Priority value - the higher the priority, the earlier the terminal gets a chance 
            // to check the input. 
            // Keywords found in grammar by default have lowest priority to allow other terminals (like identifiers)to check the input first.
            // Additionally, longer symbols have higher priority, so symbols like "+=" should have higher priority value than "+" symbol. 
            // As a result, Scanner would first try to match "+=", longer symbol, and if it fails, it will try "+". 
            // Reserved words are the opposite - they have the highest priority
            #endregion
            if (Flags.IsSet(TermFlags.IsReservedWord)) {
                Priority = TerminalPriority.ReservedWords + Text.Length; //the longer the word, the higher is the priority
            } else {
                Priority = TerminalPriority.Low + Text.Length;
            }

            SetupEditorInfo();
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            if (!source.MatchSymbol(Text)) {
                return null;
            }

            source.PreviewPosition += Text.Length;
            //In case of keywords, check that it is not followed by letter or digit
            if (Flags.IsSet(TermFlags.IsKeyword) && !AllowAlphaAfterKeyword) {
                var previewChar = source.PreviewChar;
                if (char.IsLetterOrDigit(previewChar) || previewChar == '_') {
                    return null; //reject
                }
            }
            var token = source.CreateToken(OutputTerminal, Text);
            return token;
        }

        public override IList<string> GetFirsts() {
            return new[] { Text };
        }
        public override string ToString() {
            return Name != Text ? Name : Text;
        }

        public override string TokenToString(Token token) {
            var keyw = Flags.IsSet(TermFlags.IsKeyword) ? Resources.LabelKeyword : Resources.LabelKeySymbol; //"(Keyword)" : "(Key symbol)"
            var result = (token.ValueString ?? token.Text) + " " + keyw;
            return result;
        }
        #endregion

        [DebuggerStepThrough]
        public bool Equals(KeyTerm other) {
            if (other == null) {
                return false;
            }
            return string.Equals(Text, other.Text, StringComparison.InvariantCulture);
        }

        [DebuggerStepThrough]
        public override int GetHashCode() {
            return Text.GetHashCode();
        }

        private void SetupEditorInfo() {
            //Setup editor info      
            if (EditorInfo != null) {
                return;
            }

            var tknType = TokenType.Identifier;
            if (Flags.IsSet(TermFlags.IsOperator)) {
                tknType |= TokenType.Operator;
            } else if (Flags.IsSet(TermFlags.IsDelimiter | TermFlags.IsPunctuation)) {
                tknType |= TokenType.Delimiter;
            }

            var triggers = TokenTriggers.None;
            if (Flags.IsSet(TermFlags.IsBrace)) {
                triggers |= TokenTriggers.MatchBraces;
            }

            if (Flags.IsSet(TermFlags.IsMemberSelect)) {
                triggers |= TokenTriggers.MemberSelect;
            }

            var color = TokenColor.Text;
            if (Flags.IsSet(TermFlags.IsKeyword)) {
                color = TokenColor.Keyword;
            }

            EditorInfo = new TokenEditorInfo(tknType, color, triggers);
        }

    }

}
