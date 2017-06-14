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
using System.Globalization;
using Irony.Utilities;
using JetBrains.Annotations;

namespace Irony.Parsing {

    // The purpose of this class is to provide a container for information shared 
    // between parser, scanner and token filters.
    public sealed class ParsingContext {

        public ParsingContext(Parser parser) {
            VsLineScanState = new VsScannerStateMap();
            Parser = parser;
            Language = Parser.Language;
            Culture = Language.Grammar.DefaultCulture;
            SharedParsingEventArgs = new ParsingEventArgs(this);
            SharedValidateTokenEventArgs = new ValidateTokenEventArgs(this);
        }

        public Parser Parser { get; }

        public LanguageData Language { get; }

        //Parser settings
        public ParseOptions Options { get; set; }

        public bool TracingEnabled;

        public ParseMode Mode = ParseMode.File;

        //maximum error count to report
        public static readonly int MaxErrors = 20;

        //defaults to Grammar.DefaultCulture, might be changed by app code
        public CultureInfo Culture {
            get => _culture;
            set {
                _culture = value;
                //This might be a problem for multi-threading - if we have several contexts on parallel threads with different culture.
                //Resources.Culture is static property (this is not Irony's fault, this is auto-generated file).
                Resources.Culture = value;
            }
        }

        #region Properties
        //Parser fields
        public ParseTree CurrentParseTree { get; internal set; }

        public TokenStack OpenBraces { get; } = new TokenStack();

        public ParserTrace ParserTrace { get; } = new ParserTrace();

        public ParserState CurrentParserState { get; internal set; }

        public ParseTreeNode CurrentParserInput { get; internal set; }

        //The token just scanned by Scanner
        public Token CurrentToken { get; internal set; }

        //accumulated comment tokens
        public TokenList CurrentCommentTokens { get; internal set; } = new TokenList();

        public Token PreviousToken { get; internal set; }

        //Location of last line start
        public SourceLocation PreviousLineStart { get; internal set; }

        //list for terminals - for current parser state and current input char
        public TerminalList CurrentTerminals { get; } = new TerminalList();

        public ISourceStream Source { get; internal set; }

        //State variable used in line scanning mode for VS integration
        public VsScannerStateMap VsLineScanState { get; }

        public ParserStatus Status { get; internal set; }

        // Error flag, once set remains set.
        public bool HasErrors {
            get => _hasErrors;
            internal set => _hasErrors |= value;
        }

        //values dictionary to use by custom language implementations to save some temporary values during parsing
        public readonly Dictionary<string, object> Values = new Dictionary<string, object>();

        public int TabWidth { get; set; } = 4;

        //Internal fields
        internal TokenFilterList TokenFilters { get; } = new TokenFilterList();
        internal TokenStack BufferedTokens { get; } = new TokenStack();
        internal IEnumerator<Token> FilteredTokens { get; set; } //stream of tokens after filter
        internal TokenStack PreviewTokens { get; } = new TokenStack();
        internal ParsingEventArgs SharedParsingEventArgs { get; }
        internal ValidateTokenEventArgs SharedValidateTokenEventArgs { get; }

        internal ParserStack ParserStack { get; } = new ParserStack();
        #endregion

        #region Events: TokenCreated
        public event EventHandler<ParsingEventArgs> TokenCreated;

        internal void OnTokenCreated() {
            TokenCreated?.Invoke(this, SharedParsingEventArgs);
        }
        #endregion

        #region Error handling and tracing

        [StringFormatMethod("format")]
        public Token CreateErrorToken(string format, params object[] args) {
            if (args != null && args.Length > 0) {
                format = string.Format(format, args);
            }
            return Source.CreateToken(Language.Grammar.SyntaxError, format);
        }

        [StringFormatMethod("format")]
        public void AddParserError(string format, params object[] args) {
            var location = CurrentParserInput?.Span.Location ?? Source.Location;
            HasErrors = true;
            AddParserMessage(ErrorLevel.Error, location, format, args);
        }

        [StringFormatMethod("format")]
        public void AddParserMessage(ErrorLevel level, SourceLocation location, string format, params object[] args) {
            if (CurrentParseTree == null) {
                return;
            }

            if (CurrentParseTree.ParserMessages.Count >= MaxErrors) {
                return;
            }

            if (args != null && args.Length > 0) {
                format = string.Format(format, args);
            }
            CurrentParseTree.ParserMessages.Add(new LogMessage(level, location, format, CurrentParserState));
            if (TracingEnabled) {
                AddTrace(true, format);
            }
        }

        [StringFormatMethod("format")]
        public void AddTrace(string format, params object[] args) {
            AddTrace(false, format, args);
        }

        [StringFormatMethod("format")]
        public void AddTrace(bool asError, string format, params object[] args) {
            if (!TracingEnabled) {
                return;
            }
            if (args != null && args.Length > 0) {
                format = string.Format(format, args);
            }
            ParserTrace.Add(new ParserTraceEntry(CurrentParserState, ParserStack.Top, CurrentParserInput, format, asError));
        }

        #region comments
        // Computes set of expected terms in a parser state. While there may be extended list of symbols expected at some point,
        // we want to reorganize and reduce it. For example, if the current state expects all arithmetic operators as an input,
        // it would be better to not list all operators (+, -, *, /, etc) but simply put "operator" covering them all. 
        // To achieve this grammar writer can group operators (or any other terminals) into named groups using Grammar's methods
        // AddTermReportGroup, AddNoReportGroup etc. Then instead of reporting each operator separately, Irony would include 
        // a single "group name" to represent them all.
        // The "expected report set" is not computed during parser construction (it would take considerable time), 
        // but does it on demand during parsing, when error is detected and the expected set is actually needed for error message. 
        // Multi-threading concerns. When used in multi-threaded environment (web server), the LanguageData would be shared in 
        // application-wide cache to avoid rebuilding the parser data on every request. The LanguageData is immutable, except 
        // this one case - the expected sets are constructed late by CoreParser on the when-needed basis. 
        // We don't do any locking here, just compute the set and on return from this function the state field is assigned. 
        // We assume that this field assignment is an atomic, concurrency-safe operation. The worst thing that might happen
        // is "double-effort" when two threads start computing the same set around the same time, and the last one to finish would 
        // leave its result in the state field. 
        #endregion
        internal static StringSet ComputeGroupedExpectedSetForState(Grammar grammar, ParserState state) {
            var terms = new TerminalSet();
            terms.UnionWith(state.ExpectedTerminals);
            var result = new StringSet();
            //Eliminate no-report terminals
            foreach (var group in grammar.TermReportGroups) {
                if (group.GroupType == TermReportGroupType.DoNotReport) {
                    terms.ExceptWith(group.Terminals);
                }
            }
            //Add normal and operator groups
            foreach (var group in grammar.TermReportGroups) {
                if ((group.GroupType == TermReportGroupType.Normal || group.GroupType == TermReportGroupType.Operator) &&
                    terms.Overlaps(group.Terminals)) {
                    result.Add(group.Alias);
                    terms.ExceptWith(group.Terminals);
                }
            }
            //Add remaining terminals "as is"
            foreach (var terminal in terms) {
                result.Add(terminal.ErrorAlias);
            }

            return result;
        }
        #endregion

        public void SetOption(ParseOptions option, bool set) {
            if (set) {
                Options |= option;
            } else {
                Options &= ~option;
            }
        }

        internal void Reset() {
            CurrentParserState = Parser.InitialState;
            CurrentParserInput = null;
            CurrentCommentTokens = new TokenList();
            ParserStack.Clear();
            HasErrors = false;
            ParserStack.Push(new ParseTreeNode(CurrentParserState));
            CurrentParseTree = null;
            OpenBraces.Clear();
            ParserTrace.Clear();
            CurrentTerminals.Clear();
            CurrentToken = null;
            PreviousToken = null;
            PreviousLineStart = new SourceLocation(0, -1, 0);
            BufferedTokens.Clear();
            PreviewTokens.Clear();
            Values.Clear();
            foreach (var filter in TokenFilters) {
                filter.Reset();
            }
        }

        public void SetSourceLocation(SourceLocation location) {
            foreach (var filter in TokenFilters) {
                filter.OnSetSourceLocation(location);
            }
            Source.Location = location;
        }

        public SourceSpan ComputeStackRangeSpan(int nodeCount) {
            if (nodeCount == 0) {
                return new SourceSpan(CurrentParserInput.Span.Location, 0);
            }
            var first = ParserStack[ParserStack.Count - nodeCount];
            var last = ParserStack.Top;
            return new SourceSpan(first.Span.Location, last.Span.EndPosition - first.Span.Location.Position);
        }

        #region Expected term set computations
        public StringSet GetExpectedTermSet() {
            if (CurrentParserState == null) {
                return new StringSet();
            }
            //See note about multi-threading issues in ComputeReportedExpectedSet comments.
            if (CurrentParserState.ReportedExpectedSet == null) {
                CurrentParserState.ReportedExpectedSet = ParserDataBuilder.ComputeGroupedExpectedSetForState(Language.Grammar, CurrentParserState);
            }
            //Filter out closing braces which are not expected based on previous input.
            // While the closing parenthesis ")" might be expected term in a state in general, 
            // if there was no opening parenthesis in preceding input then we would not
            //  expect a closing one. 
            var expectedSet = FilterBracesInExpectedSet(CurrentParserState.ReportedExpectedSet);
            return expectedSet;
        }

        private StringSet FilterBracesInExpectedSet(StringSet stateExpectedSet) {
            var result = new StringSet();
            result.UnionWith(stateExpectedSet);
            //Find what brace we expect
            var nextClosingBrace = string.Empty;
            if (OpenBraces.Count > 0) {
                var lastOpenBraceTerm = OpenBraces.Peek().KeyTerm;
                if (lastOpenBraceTerm.IsPairFor is KeyTerm nextClosingBraceTerm) {
                    nextClosingBrace = nextClosingBraceTerm.Text;
                }
            }
            //Now check all closing braces in result set, and leave only nextClosingBrace
            foreach (var term in Language.Grammar.KeyTerms.Values) {
                if (term.Flags.IsSet(TermFlags.IsCloseBrace)) {
                    var brace = term.Text;
                    if (result.Contains(brace) && brace != nextClosingBrace) {
                        result.Remove(brace);
                    }
                }
            }//foreach term
            return result;
        }

        #endregion

        private CultureInfo _culture;
        private bool _hasErrors;

    }

}
