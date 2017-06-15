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
using System.Linq;
using Irony.Ast;
using Irony.Utilities;

namespace Irony.Parsing {

    public sealed class StringLiteral : CompoundTerminalBase {

        #region constructors and initialization
        public StringLiteral(string name)
            : base(name) {
            SetFlag(TermFlags.IsLiteral);
        }

        public StringLiteral(string name, string startEndSymbol, StringOptions options)
            : this(name) {
            _subtypes.Add(startEndSymbol, startEndSymbol, options);
        }

        public StringLiteral(string name, string startEndSymbol)
            : this(name, startEndSymbol, StringOptions.None) {
        }

        public StringLiteral(string name, string startEndSymbol, StringOptions options, Type astNodeType)
            : this(name, startEndSymbol, options) {
            AstConfig.NodeType = astNodeType;
        }

        public StringLiteral(string name, string startEndSymbol, StringOptions options, AstNodeCreator astNodeCreator)
            : this(name, startEndSymbol, options) {
            AstConfig.NodeCreator = astNodeCreator;
        }

        public void AddStartEnd(string startEndSymbol, StringOptions stringOptions) {
            AddStartEnd(startEndSymbol, startEndSymbol, stringOptions);
        }

        public void AddStartEnd(string startSymbol, string endSymbol, StringOptions stringOptions) {
            _subtypes.Add(startSymbol, endSymbol, stringOptions);
        }

        public void AddPrefix(string prefix, StringOptions flags) {
            AddPrefixFlag(prefix, (short)flags);
        }
        #endregion

        #region StringSubType
        private sealed class StringSubType {

            internal StringSubType(string start, string end, StringOptions flags, byte index) {
                Start = start;
                End = end;
                Flags = flags;
                Index = index;
            }

            internal string Start { get; }

            internal string End { get; }

            internal StringOptions Flags { get; }

            internal byte Index { get; }

            internal static int LongerStartFirst(StringSubType x, StringSubType y) {
                return StringList.LongerFirst(x.Start, y.Start);
            }

        }

        private sealed class StringSubTypeList : List<StringSubType> {

            internal void Add(string start, string end, StringOptions flags) {
                Add(new StringSubType(start, end, flags, (byte)Count));
            }

        }
        #endregion

        #region overrides: Init, GetFirsts, ReadBody, etc...
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            _startSymbolsFirsts = new CharHashSet(CaseSensitivePrefixesSuffixes);
            if (_subtypes.Count == 0) {
                //"Error in string literal [{0}]: No start/end symbols specified."
                grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrInvStrDef, Name);
                return;
            }
            //collect all start-end symbols in lists and create strings of first chars
            var allStartSymbols = new StringSet(); //to detect duplicate start symbols
            _subtypes.Sort(StringSubType.LongerStartFirst);
            var isTemplate = false;
            foreach (var subType in _subtypes) {
                if (allStartSymbols.Contains(subType.Start)) {
                    //"Duplicate start symbol {0} in string literal [{1}]."
                    grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrDupStartSymbolStr, subType.Start, Name);
                }

                allStartSymbols.Add(subType.Start);
                _startSymbolsFirsts.Add(subType.Start[0]);
                if ((subType.Flags & StringOptions.IsTemplate) != 0) {
                    isTemplate = true;
                }
            }

            //Set multiline flag
            if (_subtypes.Any(info => (info.Flags & StringOptions.AllowsLineBreak) != 0)) {
                SetFlag(TermFlags.IsMultiline);
            }

            //For templates only
            if (isTemplate) {
                //Check that template settings object is provided
                var templateSettings = AstConfig.Data as StringTemplateSettings;
                if (templateSettings == null) {
                    //"Error in string literal [{0}]: IsTemplate flag is set, but TemplateSettings is not provided."
                    grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrTemplNoSettings, Name);
                } else if (templateSettings.ExpressionRoot == null) {
                    //""
                    grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrTemplMissingExprRoot, Name);
                } else if (!Grammar.SnippetRoots.Contains(templateSettings.ExpressionRoot)) {
                    //""
                    grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrTemplExprNotRoot, Name);
                }
            }

            //Create editor info
            if (EditorInfo == null) {
                EditorInfo = new TokenEditorInfo(TokenType.String, TokenColor.String, TokenTriggers.None);
            }
        }

        public override IList<string> GetFirsts() {
            var result = new StringList();
            result.AddRange(Prefixes);
            //we assume that prefix is always optional, so string can start with start-end symbol
            result.AddRange(_startSymbolsFirsts.Select(ch => ch.ToString()));

            return result;
        }

        protected override bool ReadBody(ISourceStream source, CompoundTokenDetails details) {
            if (!details.PartialContinues) {
                if (!ReadStartSymbol(source, details)) {
                    return false;
                }
            }
            return CompleteReadBody(source, details);
        }

        private bool CompleteReadBody(ISourceStream source, CompoundTokenDetails details) {
            var escapeEnabled = !details.IsSet((short)StringOptions.NoEscapes);
            var start = source.PreviewPosition;
            var endQuoteSymbol = details.EndSymbol;
            var endQuoteDoubled = endQuoteSymbol + endQuoteSymbol; //doubled quote symbol
            var lineBreakAllowed = details.IsSet((short)StringOptions.AllowsLineBreak);
            //1. Find the string end
            // first get the position of the next line break; we are interested in it to detect malformed string, 
            //  therefore do it only if linebreak is NOT allowed; if linebreak is allowed, set it to -1 (we don't care).  
            var nlPos = lineBreakAllowed ? -1 : source.Text.IndexOf('\n', source.PreviewPosition);

            //fix by ashmind for EOF right after opening symbol
            while (true) {
                var endPos = source.Text.IndexOf(endQuoteSymbol, source.PreviewPosition, StringComparison.InvariantCulture);
                //Check for partial token in line-scanning mode
                if (endPos < 0 && details.PartialOk && lineBreakAllowed) {
                    ProcessPartialBody(source, details);
                    return true;
                }
                //Check for malformed string: either EndSymbol not found, or LineBreak is found before EndSymbol
                var malformed = endPos < 0 || nlPos >= 0 && nlPos < endPos;
                if (malformed) {
                    //Set source position for recovery: move to the next line if linebreak is not allowed.
                    if (nlPos > 0) {
                        endPos = nlPos;
                    }

                    if (endPos > 0) {
                        source.PreviewPosition = endPos + 1;
                    }

                    details.Error = Resources.ErrBadStrLiteral;//    "Mal-formed  string literal - cannot find termination symbol.";
                    return true; //we did find start symbol, so it is definitely string, only malformed
                }

                if (source.EOF) {
                    return true;
                }

                //We found EndSymbol - check if it is escaped; if yes, skip it and continue search
                if (escapeEnabled && IsEndQuoteEscaped(source.Text, endPos)) {
                    source.PreviewPosition = endPos + endQuoteSymbol.Length;
                    continue; //searching for end symbol
                }

                //Check if it is doubled end symbol
                source.PreviewPosition = endPos;
                if (details.IsSet((short)StringOptions.AllowsDoubledQuote) && source.MatchSymbol(endQuoteDoubled)) {
                    source.PreviewPosition = endPos + endQuoteDoubled.Length;
                    continue;
                }

                //Ok, this is normal endSymbol that terminates the string. 
                // Advance source position and get out from the loop
                details.Body = source.Text.Substring(start, endPos - start);
                source.PreviewPosition = endPos + endQuoteSymbol.Length;
                //if we come here it means we're done - we found string end.
                return true;
            }
        }

        private static void ProcessPartialBody(ISourceStream source, CompoundTokenDetails details) {
            var from = source.PreviewPosition;
            source.PreviewPosition = source.Text.Length;
            details.Body = source.Text.Substring(from, source.PreviewPosition - from);
            details.IsPartial = true;
        }

        protected override void InitDetails(ParsingContext context, CompoundTokenDetails details) {
            base.InitDetails(context, details);
            if (context.VsLineScanState.Value == 0) {
                return;
            }
            //we are continuing partial string on the next line
            details.Flags = context.VsLineScanState.TerminalFlags;
            details.SubTypeIndex = context.VsLineScanState.TokenSubType;
            var stringInfo = _subtypes[context.VsLineScanState.TokenSubType];
            details.StartSymbol = stringInfo.Start;
            details.EndSymbol = stringInfo.End;
        }

        protected override void ReadSuffix(ISourceStream source, CompoundTokenDetails details) {
            base.ReadSuffix(source, details);
            //"char" type can be identified by suffix (like VB where c suffix identifies char)
            // in this case we have details.TypeCodes[0] == char  and we need to set the IsChar flag
            if (details.TypeCodes != null && details.TypeCodes[0] == TypeCode.Char) {
                details.Flags |= (int)StringOptions.IsChar;
            } else if (details.IsSet((short)StringOptions.IsChar)) {
                //we may have IsChar flag set (from startEndSymbol, like in c# single quote identifies char)
                // in this case set type code
                details.TypeCodes = new[] { TypeCode.Char };
            }
        }

        private bool IsEndQuoteEscaped(string text, int quotePosition) {
            var escaped = false;
            var p = quotePosition - 1;
            while (p > 0 && text[p] == EscapeChar) {
                escaped = !escaped;
                p--;
            }
            return escaped;
        }

        private bool ReadStartSymbol(ISourceStream source, CompoundTokenDetails details) {
            if (!_startSymbolsFirsts.Contains(source.PreviewChar)) {
                return false;
            }

            foreach (var subType in _subtypes) {
                if (!source.MatchSymbol(subType.Start)) {
                    continue;
                }
                //We found start symbol
                details.StartSymbol = subType.Start;
                details.EndSymbol = subType.End;
                details.Flags |= (short)subType.Flags;
                details.SubTypeIndex = subType.Index;
                source.PreviewPosition += subType.Start.Length;
                return true;
            }
            return false;
        }


        //Extract the string content from lexeme, adjusts the escaped and double-end symbols
        protected override bool ConvertValue(CompoundTokenDetails details) {
            var value = details.Body;
            var escapeEnabled = !details.IsSet((short)StringOptions.NoEscapes);
            //Fix all escapes
            if (escapeEnabled && value.IndexOf(EscapeChar) >= 0) {
                details.Flags |= (int)StringFlagsInternal.HasEscapes;
                var arr = value.Split(EscapeChar);
                var ignoreNext = false;
                //we skip the 0 element as it is not preceeded by "\"
                for (var i = 1; i < arr.Length; i++) {
                    if (ignoreNext) {
                        ignoreNext = false;
                        continue;
                    }
                    var s = arr[i];
                    if (string.IsNullOrEmpty(s)) {
                        //it is "\\" - escaped escape symbol. 
                        arr[i] = @"\";
                        ignoreNext = true;
                        continue;
                    }
                    //The char is being escaped is the first one; replace it with char in Escapes table
                    var first = s[0];
                    if (Escapes.TryGetValue(first, out char newFirst)) {
                        arr[i] = newFirst + s.Substring(1);
                    } else {
                        arr[i] = HandleSpecialEscape(arr[i], details);
                    }
                }
                value = string.Join(string.Empty, arr);
            }

            //Check for doubled end symbol
            var endSymbol = details.EndSymbol;
            if (details.IsSet((short)StringOptions.AllowsDoubledQuote) && value.IndexOf(endSymbol, StringComparison.InvariantCulture) >= 0) {
                value = value.Replace(endSymbol + endSymbol, endSymbol);
            }

            if (details.IsSet((short)StringOptions.IsChar)) {
                if (value.Length != 1) {
                    details.Error = Resources.ErrBadChar;  //"Invalid length of char literal - should be a single character.";
                    return false;
                }
                details.Value = value[0];
            } else {
                details.TypeCodes = new[] { TypeCode.String };
                details.Value = value;
            }
            return true;
        }

        //Should support:  \Udddddddd, \udddd, \xdddd, \N{name}, \0, \ddd (octal),  
        private static string HandleSpecialEscape(string segment, CompoundTokenDetails details) {
            if (string.IsNullOrEmpty(segment)) {
                return string.Empty;
            }

            int p;
            string digits; char ch; string result;
            var first = segment[0];
            switch (first) {
                case 'u':
                case 'U':
                    if (details.IsSet((short)StringOptions.AllowsUEscapes)) {
                        var len = (first == 'u' ? 4 : 8);
                        if (segment.Length < len + 1) {
                            details.Error = string.Format(Resources.ErrBadUnEscape, segment.Substring(len + 1), len);// "Invalid unicode escape ({0}), expected {1} hex digits."
                            return segment;
                        }
                        digits = segment.Substring(1, len);
                        ch = (char)Convert.ToUInt32(digits, 16);
                        result = ch + segment.Substring(len + 1);
                        return result;
                    }
                    break;
                case 'x':
                    if (details.IsSet((short)StringOptions.AllowsXEscapes)) {
                        //x-escape allows variable number of digits, from one to 4; let's count them
                        p = 1; //current position
                        while (p < 5 && p < segment.Length) {
                            if (Strings.HexDigits.IndexOf(segment[p]) < 0) {
                                break;
                            }

                            p++;
                        }
                        //p now point to char right after the last digit
                        if (p <= 1) {
                            details.Error = Resources.ErrBadXEscape; // @"Invalid \x escape, at least one digit expected.";
                            return segment;
                        }
                        digits = segment.Substring(1, p - 1);
                        ch = (char)Convert.ToUInt32(digits, 16);
                        result = ch + segment.Substring(p);
                        return result;
                    }
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                    if (details.IsSet((short)StringOptions.AllowsOctalEscapes)) {
                        //octal escape allows variable number of digits, from one to 3; let's count them
                        p = 0; //current position
                        while (p < 3 && p < segment.Length) {
                            if (Strings.OctalDigits.IndexOf(segment[p]) < 0) {
                                break;
                            }

                            p++;
                        }
                        //p now point to char right after the last digit
                        digits = segment.Substring(0, p);
                        ch = (char)Convert.ToUInt32(digits, 8);
                        result = ch + segment.Substring(p);
                        return result;
                    }
                    break;
            }
            details.Error = string.Format(Resources.ErrInvEscape, segment); //"Invalid escape sequence: \{0}"
            return segment;
        }
        #endregion

        private readonly StringSubTypeList _subtypes = new StringSubTypeList();
        //first chars  of start-end symbols
        private CharHashSet _startSymbolsFirsts;

    }

}
