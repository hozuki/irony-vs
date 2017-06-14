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
using Irony.Utilities;

#region notes
//Identifier terminal. Matches alpha-numeric sequences that usually represent identifiers and keywords.
// c#: @ prefix signals to not interpret as a keyword; allows \u escapes
// 
#endregion

namespace Irony.Parsing {

    public sealed class IdentifierTerminal : CompoundTerminalBase {

        #region Constructors
        public IdentifierTerminal(string name)
            : this(name, IdOptions.None) {
        }

        public IdentifierTerminal(string name, IdOptions options)
            : this(name, "_", "_") {
            Options = options;
        }

        public IdentifierTerminal(string name, string extraChars)
            : this(name, extraChars, string.Empty) {
        }

        public IdentifierTerminal(string name, string extraChars, string extraFirstChars)
            : base(name) {
            AllFirstChars = Strings.AllLatinLetters + extraFirstChars;
            AllChars = Strings.AllLatinLetters + Strings.DecimalDigits + extraChars;
        }
        #endregion

        #region properties: AllChars, AllFirstChars
        public string AllFirstChars { get; }

        public string AllChars { get; }

        public TokenEditorInfo KeywordEditorInfo { get; } = new TokenEditorInfo(TokenType.Keyword, TokenColor.Keyword, TokenTriggers.None);

        //flags for the case when there are no prefixes
        public IdOptions Options { get; }

        public CaseRestriction CaseRestriction { get; set; }

        //categories of first char
        public UnicodeCategoryList StartCharCategories { get; } = new UnicodeCategoryList();

        //categories of all other chars
        public UnicodeCategoryList CharCategories { get; } = new UnicodeCategoryList();

        //categories of chars to remove from final id, usually formatting category
        public UnicodeCategoryList CharsToRemoveCategories { get; } = new UnicodeCategoryList();
        #endregion

        #region Overrides
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            _allCharsSet = new CharHashSet(Grammar.CaseSensitive);
            _allCharsSet.UnionWith(AllChars.ToCharArray());

            //Adjust case restriction. We adjust only first chars; if first char is ok, we will scan the rest without restriction 
            // and then check casing for entire identifier
            switch (CaseRestriction) {
                case CaseRestriction.AllLower:
                case CaseRestriction.FirstLower:
                    _allFirstCharsSet = new CharHashSet(true);
                    _allFirstCharsSet.UnionWith(AllFirstChars.ToLowerInvariant().ToCharArray());
                    break;
                case CaseRestriction.AllUpper:
                case CaseRestriction.FirstUpper:
                    _allFirstCharsSet = new CharHashSet(true);
                    _allFirstCharsSet.UnionWith(AllFirstChars.ToUpperInvariant().ToCharArray());
                    break;
                default:
                    //None
                    _allFirstCharsSet = new CharHashSet(Grammar.CaseSensitive);
                    _allFirstCharsSet.UnionWith(AllFirstChars.ToCharArray());
                    break;
            }
            //if there are "first" chars defined by categories, add the terminal to FallbackTerminals
            if (StartCharCategories.Count > 0) {
                grammarData.NoPrefixTerminals.Add(this);
            }

            if (EditorInfo == null) {
                EditorInfo = new TokenEditorInfo(TokenType.Identifier, TokenColor.Identifier, TokenTriggers.None);
            }
        }

        public override IList<string> GetFirsts() {
            // new scanner: identifier has no prefixes
            return null;
            /*
                  var list = new StringList();
                  list.AddRange(Prefixes);
                  foreach (char ch in _allFirstCharsSet)
                    list.Add(ch.ToString());
                  if ((Options & IdOptions.CanStartWithEscape) != 0)
                    list.Add(this.EscapeChar.ToString());
                  return list;
             */
        }

        public void AddPrefix(string prefix, IdOptions options) {
            AddPrefixFlag(prefix, (short)options);
        }

        protected override void InitDetails(ParsingContext context, CompoundTokenDetails details) {
            base.InitDetails(context, details);
            details.Flags = (short)Options;
        }

        //Override to assign IsKeyword flag to keyword tokens
        protected override Token CreateToken(ParsingContext context, ISourceStream source, CompoundTokenDetails details) {
            var token = base.CreateToken(context, source, details);
            if (details.IsSet((short)IdOptions.IsNotKeyword)) {
                return token;
            }
            //check if it is keyword
            CheckReservedWord(token);
            return token;
        }

        protected override Token QuickParse(ParsingContext context, ISourceStream source) {
            if (!_allFirstCharsSet.Contains(source.PreviewChar)) {
                return null;
            }

            source.PreviewPosition++;
            while (_allCharsSet.Contains(source.PreviewChar) && !source.EOF) {
                source.PreviewPosition++;
            }
            //if it is not a terminator then cancel; we need to go through full algorithm
            if (!Grammar.IsWhitespaceOrDelimiter(source.PreviewChar)) {
                return null;
            }

            var token = source.CreateToken(OutputTerminal);
            if (CaseRestriction != CaseRestriction.None && !CheckCaseRestriction(token.ValueString)) {
                return null;
            }
            //!!! Do not convert to common case (all-lower) for case-insensitive grammar. Let identifiers remain as is, 
            //  it is responsibility of interpreter to provide case-insensitive read/write operations for identifiers
            // if (!this.GrammarData.Grammar.CaseSensitive)
            //    token.Value = token.Text.ToLower(CultureInfo.InvariantCulture);
            CheckReservedWord(token);
            return token;
        }

        protected override bool ReadBody(ISourceStream source, CompoundTokenDetails details) {
            var start = source.PreviewPosition;
            var allowEscapes = details.IsSet((short)IdOptions.AllowsEscapes);
            var outputChars = new CharList();
            while (!source.EOF) {
                var current = source.PreviewChar;
                if (Grammar.IsWhitespaceOrDelimiter(current)) {
                    break;
                }

                if (allowEscapes && current == EscapeChar) {
                    current = ReadUnicodeEscape(source, details);
                    //We  need to back off the position. ReadUnicodeEscape sets the position to symbol right after escape digits.  
                    //This is the char that we should process in next iteration, so we must backup one char, to pretend the escaped
                    // char is at position of last digit of escape sequence. 
                    source.PreviewPosition--;
                    if (details.Error != null) {
                        return false;
                    }
                }
                //Check if current character is OK
                if (!IsCharOk(current, source.PreviewPosition == start)) {
                    break;
                }
                //Check if we need to skip this char
                var currCat = char.GetUnicodeCategory(current); //I know, it suxx, we do it twice, fix it later
                if (!CharsToRemoveCategories.Contains(currCat)) {
                    outputChars.Add(current); //add it to output (identifier)
                }

                source.PreviewPosition++;
            }
            if (outputChars.Count == 0) {
                return false;
            }
            //Convert collected chars to string
            details.Body = new string(outputChars.ToArray());
            if (!CheckCaseRestriction(details.Body)) {
                return false;
            }

            return !string.IsNullOrEmpty(details.Body);
        }

        private void CheckReservedWord(Token token) {
            if (!Grammar.KeyTerms.TryGetValue(token.Text, out KeyTerm keyTerm)) {
                return;
            }
            token.KeyTerm = keyTerm;
            //if it is reserved word, then overwrite terminal
            if (keyTerm.Flags.IsSet(TermFlags.IsReservedWord)) {
                token.SetTerminal(keyTerm);
            }
        }

        private bool IsCharOk(char ch, bool first) {
            //first check char lists, then categories
            var charSet = first ? _allFirstCharsSet : _allCharsSet;
            if (charSet.Contains(ch)) {
                return true;
            }
            //check categories
            if (CharCategories.Count > 0) {
                var chCat = char.GetUnicodeCategory(ch);
                var catList = first ? StartCharCategories : CharCategories;
                if (catList.Contains(chCat)) {
                    return true;
                }
            }
            return false;
        }

        private bool CheckCaseRestriction(string body) {
            switch (CaseRestriction) {
                case CaseRestriction.FirstLower:
                    return char.IsLower(body, 0);
                case CaseRestriction.FirstUpper:
                    return char.IsUpper(body, 0);
                case CaseRestriction.AllLower:
                    return body.ToLower() == body;
                case CaseRestriction.AllUpper:
                    return body.ToUpper() == body;
                default:
                    return true;
            }
        }


        private static char ReadUnicodeEscape(ISourceStream source, CompoundTokenDetails details) {
            //Position is currently at "\" symbol
            source.PreviewPosition++; //move to U/u char
            int len;
            switch (source.PreviewChar) {
                case 'u':
                    len = 4;
                    break;
                case 'U':
                    len = 8;
                    break;
                default:
                    details.Error = Resources.ErrInvEscSymbol; // "Invalid escape symbol, expected 'u' or 'U' only."
                    return '\0';
            }
            if (source.PreviewPosition + len > source.Text.Length) {
                details.Error = Resources.ErrInvEscSeq; // "Invalid escape sequence";
                return '\0';
            }
            source.PreviewPosition++; //move to the first digit
            var digits = source.Text.Substring(source.PreviewPosition, len);
            var result = (char)Convert.ToUInt32(digits, 16);
            source.PreviewPosition += len;
            details.Flags |= (int)IdFlagsInternal.HasEscapes;
            return result;
        }

        protected override bool ConvertValue(CompoundTokenDetails details) {
            if (details.IsSet((short)IdOptions.NameIncludesPrefix)) {
                details.Value = details.Prefix + details.Body;
            } else {
                details.Value = details.Body;
            }
            return true;
        }
        #endregion

        private CharHashSet _allCharsSet;
        private CharHashSet _allFirstCharsSet;

    }

}
