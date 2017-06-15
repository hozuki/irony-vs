using System;
using Irony.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Ast;
using JetBrains.Annotations;

namespace Irony.Parsing {

    public sealed class HereDocTerminal : CompoundTerminalBase {

        #region HereDocSubType
        private sealed class HereDocSubType {

            /// <summary>
            /// 
            /// </summary>
            /// <param name="start"></param>
            /// <param name="quotes">e.g. single quote/double quote/backquote./</param>
            /// <param name="flags"></param>
            /// <param name="index"></param>
            internal HereDocSubType(string start, ISet<string> quotes, HereDocOptions flags, byte index) {
                Start = start;
                Quotes = new StringSet();
                if (quotes != null) {
                    Quotes.UnionWith(quotes);
                }
                Flags = flags;
                Index = index;
            }

            internal HereDocSubType(string start, HereDocOptions flags, byte index)
                : this(start, null, flags, index) {
            }

            public string Start { get; }

            [NotNull]
            public StringSet Quotes { get; }

            public HereDocOptions Flags { get; }

            public byte Index { get; internal set; }

            public HereDocSubType Clone() {
                return new HereDocSubType(Start, Quotes, Flags, Index);
            }

            internal static int LongerStartFirst(HereDocSubType x, HereDocSubType y) {
                return StringList.LongerFirst(x.Start, y.Start);
            }

        }

        private sealed class HereDocSubTypeList : List<HereDocSubType> {

            internal void Add(string start, HereDocOptions options) {
                Add(new HereDocSubType(start, options, (byte)Count));
            }

            internal void Add(string start, ISet<string> quotes, HereDocOptions options) {
                Add(new HereDocSubType(start, quotes, options, (byte)Count));
            }

        }
        #endregion

        #region Constructors
        public HereDocTerminal(string name, string start)
            : this(name, start, HereDocOptions.None) {
        }

        public HereDocTerminal(string name, string start, HereDocOptions options)
            : base(name) {
            _subTypes.Add(start, options);
        }

        public HereDocTerminal(string name, string start, HereDocOptions options, AstNodeCreator astNodeCreator)
            : this(name, start, options) {
            AstConfig.NodeCreator = astNodeCreator;
        }

        public HereDocTerminal(string name, string start, HereDocOptions options, Type astNodeType)
            : this(name, start, options) {
            AstConfig.NodeType = astNodeType;
        }

        public HereDocTerminal(string name, string start, ISet<string> quotes)
            : this(name, start, quotes, HereDocOptions.None) {
        }

        public HereDocTerminal(string name, string start, ISet<string> quotes, HereDocOptions options)
            : base(name) {
            _subTypes.Add(start, quotes, options);
        }

        public HereDocTerminal(string name, string start, ISet<string> quotes, HereDocOptions options, AstNodeCreator astNodeCreator)
            : this(name, start, quotes, options) {
            AstConfig.NodeCreator = astNodeCreator;
        }

        public HereDocTerminal(string name, string start, ISet<string> quotes, HereDocOptions options, Type astNodeType)
            : this(name, start, quotes, options) {
            AstConfig.NodeType = astNodeType;
        }

        public void AddSubType(string start, HereDocOptions options) {
            _subTypes.Add(start, options);
        }

        public void AddSubType(string start, ISet<string> quotes, HereDocOptions options) {
            _subTypes.Add(start, quotes, options);
        }
        #endregion

        public bool QuoteCaseSensitive { get; set; } = true;

        #region overrides: Init, GetFirsts, ReadBody, etc...
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);

            _startSymbolsFirsts = new CharHashSet(CaseSensitivePrefixesSuffixes);

            if (_subTypes.Count == 0) {
                //"Error in heredoc literal [{0}]: No start/end symbols specified."
                grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrInvHereDocDef, Name);
                return;
            }

            MergeSubTypes();

            _subTypes.Sort(HereDocSubType.LongerStartFirst);

            var quoteStrings = new StringSet(QuoteCaseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase);
            foreach (var subType in _subTypes) {
                if (quoteStrings.Overlaps(subType.Quotes)) {
                    //"Duplicate start symbol {0} in heredoc literal [{1}]."
                    grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrDupStartSymbolHereDoc, subType.Start, Name);
                }
                quoteStrings.UnionWith(subType.Quotes);
            }

            var allStartSymbols = new StringSet();
            var isTemplate = false;
            foreach (var subType in _subTypes) {
                if (allStartSymbols.Contains(subType.Start)) {
                    //"Duplicate start symbol {0} in heredoc literal [{1}]."
                    grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrDupStartSymbolHereDoc, subType.Start, Name);
                }

                allStartSymbols.Add(subType.Start);
                _startSymbolsFirsts.Add(subType.Start[0]);
                if ((subType.Flags & HereDocOptions.IsTemplate) != 0) {
                    isTemplate = true;
                }
            }

            // Always allow multiline.
            SetFlag(TermFlags.IsMultiline);

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
            var escapeEnabled = !details.IsSet((short)HereDocOptions.NoEscapes);
            var start = source.PreviewPosition;
            var endQuoteSymbol = details.TokenName;
            //1. Find the string end
            // first get the position of the next line break; we are interested in it to detect malformed string, 
            //  therefore do it only if linebreak is NOT allowed; if linebreak is allowed, set it to -1 (we don't care).
            // MIC: in heredoc, multiline is always allowed.

            //fix by ashmind for EOF right after opening symbol
            while (true) {
                var endPos = source.Text.IndexOf(endQuoteSymbol, source.PreviewPosition, StringComparison.InvariantCulture);
                //Check for partial token in line-scanning mode
                if (endPos < 0 && details.PartialOk) {
                    ProcessPartialBody(source, details);
                    return true;
                }
                //Check for malformed string: either EndSymbol not found, or LineBreak is found before EndSymbol
                var malformed = endPos < 0;
                if (malformed) {
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

                source.PreviewPosition = endPos;

                //Ok, this is normal endSymbol that terminates the string. 
                // Advance source position and get out from the loop
                source.PreviewPosition = endPos + endQuoteSymbol.Length;
                // Remove the last newline.
                // Text[endPos-1] is always \n.
                if (source.Text[endPos - 2] == '\r') {
                    endPos -= 2;
                } else {
                    endPos -= 1;
                }
                if (details.IsSet((short)HereDocOptions.RemoveBeginningNewLine)) {
                    if (source.Text[start] == '\r') {
                        start += 2;
                    } else {
                        start += 1;
                    }
                }
                details.Body = source.Text.Substring(start, endPos - start);
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
            var stringInfo = _subTypes[context.VsLineScanState.TokenSubType];
            details.StartSymbol = stringInfo.Start;
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

            foreach (var subType in _subTypes) {
                if (!source.MatchSymbol(subType.Start)) {
                    continue;
                }

                var previewPos = source.PreviewPosition;
                source.PreviewPosition += subType.Start.Length;
                Grammar.SkipWhitespace(source, true);

                string quote = null;
                // Search if there should be a quote.
                if (subType.Quotes.Count > 0) {
                    // Must be quoted.
                    var quoteMatchSuccessful = false;
                    foreach (var q in subType.Quotes) {
                        // TODO: what if not case sensitive?
                        if (!source.MatchSymbol(q)) {
                            continue;
                        }
                        quoteMatchSuccessful = true;
                        quote = q;
                    }
                    if (!quoteMatchSuccessful) {
                        // Revert, revert!
                        source.PreviewPosition = previewPos;
                        continue;
                    }
                }

                // Now the preview position is at the beginning of quotes, or the name.
                var sb = new StringBuilder();
                var previewChar = source.PreviewChar;
                while (previewChar != '\r' && previewChar != '\n') {
                    sb.Append(previewChar);
                    ++source.PreviewPosition;
                    previewChar = source.PreviewChar;
                }
                var endLiteral = sb.ToString();
                if (quote != null) {
                    var comparisonType = QuoteCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
                    // Check the quotes.
                    if (!(endLiteral.StartsWith(quote, comparisonType) && endLiteral.EndsWith(quote, comparisonType))) {
                        // Malformed end literal.
                        return false;
                    } else {
                        endLiteral = endLiteral.Substring(quote.Length, endLiteral.Length - quote.Length * 2);
                    }
                }

                int elStart = 0, elEnd = endLiteral.Length - 1;
                // Trim
                while (elStart < endLiteral.Length && Grammar.IsWhitespace(endLiteral[elStart])) {
                    elStart++;
                }
                while (elEnd >= 0 && Grammar.IsWhitespace(endLiteral[elEnd])) {
                    elStart--;
                }
                if (elEnd <= elStart) {
                    // Malformed end literal.
                    return false;
                }

                if (elStart != 0 || elEnd != endLiteral.Length - 1) {
                    endLiteral = endLiteral.Substring(elStart, elEnd - elStart + 1);
                }

                //We found start symbol
                details.StartSymbol = subType.Start;
                details.Flags |= (short)subType.Flags;
                details.TokenName = endLiteral;
                details.TokenQuote = quote;
                details.SubTypeIndex = subType.Index;
                // No need to set source.PreviewPosition, we've done it.
                return true;
            }
            return false;
        }

        //Extract the string content from lexeme, adjusts the escaped and double-end symbols
        protected override bool ConvertValue(CompoundTokenDetails details) {
            var value = details.Body;
            var escapeEnabled = !details.IsSet((short)HereDocOptions.NoEscapes);
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

            details.TypeCodes = new[] { TypeCode.String };
            details.Value = value;

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
                    if (details.IsSet((short)HereDocOptions.AllowsUEscapes)) {
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
                    if (details.IsSet((short)HereDocOptions.AllowsXEscapes)) {
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
                    if (details.IsSet((short)HereDocOptions.AllowsOctalEscapes)) {
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

        private void MergeSubTypes() {
            var newList = new HereDocSubTypeList();
            var comparer = QuoteCaseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase;
            var merged = false;
            foreach (var subtype in _subTypes) {
                var existItem = newList.Find(st => comparer.Compare(st.Start, subtype.Start) == 0 && st.Flags == subtype.Flags);
                if (existItem == null) {
                    newList.Add(subtype.Clone());
                    continue;
                }
                // Let's merge!
                existItem.Quotes.UnionWith(subtype.Quotes);
                merged = true;
            }
            if (merged) {
                // Adjust indices.
                var i = 0;
                foreach (var subtype in newList) {
                    subtype.Index = (byte)i++;
                }
                _subTypes.Clear();
                _subTypes.AddRange(newList);
            }
        }

        private HereDocSubTypeList _subTypes = new HereDocSubTypeList();

        private CharHashSet _startSymbolsFirsts;

    }

}
