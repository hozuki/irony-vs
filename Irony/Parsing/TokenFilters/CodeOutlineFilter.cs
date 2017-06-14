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

using System.Collections.Generic;
using Irony.Utilities;

namespace Irony.Parsing {

    public sealed class CodeOutlineFilter : TokenFilter {

        #region constructor
        public CodeOutlineFilter(GrammarData grammarData, OutlineOptions options, KeyTerm continuationTerminal) {
            _grammar = grammarData.Grammar;
            _grammar.LanguageFlags |= LanguageFlags.EmitLineStartToken;
            Options = options;
            ContinuationTerminal = continuationTerminal;
            if (ContinuationTerminal != null) {
                if (!_grammar.NonGrammarTerminals.Contains(ContinuationTerminal)) {
                    grammarData.Language.Errors.Add(GrammarErrorLevel.Warning, null, Resources.ErrOutlineFilterContSymbol, ContinuationTerminal.Name);
                }
            }
            //"CodeOutlineFilter: line continuation symbol '{0}' should be added to Grammar.NonGrammarTerminals list.",
            _produceIndents = IsSet(OutlineOptions.ProduceIndents);
            _checkBraces = IsSet(OutlineOptions.CheckBraces);
            _checkOperator = IsSet(OutlineOptions.CheckOperator);
            Reset();
        }
        #endregion

        public OutlineOptions Options { get; }

        //Terminal
        public KeyTerm ContinuationTerminal { get; }

        public Stack<int> Indents { get; } = new Stack<int>();

        public Token CurrentToken { get; internal set; }

        public Token PreviousToken { get; internal set; }

        public SourceLocation PreviousTokenLocation { get; private set; }

        public TokenStack OutputTokens { get; } = new TokenStack();

        public override void Reset() {
            Indents.Clear();
            Indents.Push(0);
            OutputTokens.Clear();
            PreviousToken = null;
            CurrentToken = null;
            PreviousTokenLocation = new SourceLocation();
        }

        public bool IsSet(OutlineOptions option) {
            return (Options & option) != 0;
        }

        public override IEnumerable<Token> BeginFiltering(ParsingContext context, IEnumerable<Token> tokens) {
            _context = context;
            foreach (var token in tokens) {
                ProcessToken(token);
                while (OutputTokens.Count > 0) {
                    yield return OutputTokens.Pop();
                }
            }
        }

        public void ProcessToken(Token token) {
            SetCurrentToken(token);
            //Quick checks
            if (_isContinuation) {
                return;
            }

            var tokenTerm = token.Terminal;

            //check EOF
            if (tokenTerm == _grammar.Eof) {
                ProcessEofToken();
                return;
            }

            if (tokenTerm != _grammar.LineStartTerminal) {
                return;
            }
            //if we are here, we have LineStart token on new line; first remove it from stream, it should not go to parser
            OutputTokens.Pop();

            if (PreviousToken == null) {
                return;
            }


            // first check if there was continuation symbol before
            // or - if checkBraces flag is set - check if there were open braces
            if (_prevIsContinuation || _checkBraces && _context.OpenBraces.Count > 0) {
                return; //no Eos token in this case
            }

            if (_prevIsOperator && _checkOperator) {
                return; //no Eos token in this case
            }

            //We need to produce Eos token and indents (if _produceIndents is set). 
            // First check indents - they go first into OutputTokens stack, so they will be popped out last
            if (_produceIndents) {
                var currIndent = token.Location.Column;
                var prevIndent = Indents.Peek();
                if (currIndent > prevIndent) {
                    Indents.Push(currIndent);
                    PushOutlineToken(_grammar.Indent, token.Location);
                } else if (currIndent < prevIndent) {
                    PushDedents(currIndent);
                    //check that current indent exactly matches the previous indent 
                    if (Indents.Peek() != currIndent) {
                        //fire error
                        OutputTokens.Push(new Token(_grammar.SyntaxError, token.Location, string.Empty, Resources.ErrInvDedent));
                        // "Invalid dedent level, no previous matching indent found."
                    }
                }
            }

            //Finally produce Eos token, but not in command line mode. In command line mode the Eos was already produced 
            // when we encountered Eof on previous line
            if (_context.Mode != ParseMode.CommandLine) {
                var eosLocation = ComputeEosLocation();
                PushOutlineToken(_grammar.Eos, eosLocation);
            }
        }

        private void SetCurrentToken(Token token) {
            _doubleEof = CurrentToken != null && CurrentToken.Terminal == _grammar.Eof
                         && token.Terminal == _grammar.Eof;
            //Copy CurrentToken to PreviousToken
            if (CurrentToken != null && CurrentToken.Category == TokenCategory.Content) { //remember only content tokens
                PreviousToken = CurrentToken;
                _prevIsContinuation = _isContinuation;
                _prevIsOperator = _isOperator;
                if (PreviousToken != null) {
                    PreviousTokenLocation = PreviousToken.Location;
                }
            }
            CurrentToken = token;
            _isContinuation = (token.Terminal == ContinuationTerminal && ContinuationTerminal != null);
            _isOperator = token.Terminal.Flags.IsSet(TermFlags.IsOperator);
            if (!_isContinuation) {
                OutputTokens.Push(token); //by default input token goes to output, except continuation symbol
            }
        }

        //Processes Eof token. We should take into account the special case of processing command line input. 
        // In this case we should not automatically dedent all stacked indents if we get EOF.
        // Note that tokens will be popped from the OutputTokens stack and sent to parser in the reverse order compared to 
        // the order we pushed them into OutputTokens stack. We have Eof already in stack; we first push dedents, then Eos
        // They will come out to parser in the following order: Eos, Dedents, Eof.
        private void ProcessEofToken() {
            //First decide whether we need to produce dedents and Eos symbol
            bool pushDedents;
            var pushEos = true;
            switch (_context.Mode) {
                case ParseMode.File:
                    pushDedents = _produceIndents; //Do dedents if token filter tracks indents
                    break;
                case ParseMode.CommandLine:
                    //only if user entered empty line, we dedent all
                    pushDedents = _produceIndents && _doubleEof;
                    pushEos = !_prevIsContinuation && !_doubleEof; //if previous symbol is continuation symbol then don't push Eos
                    break;
                case ParseMode.VsLineScan:
                    pushDedents = false; //Do not dedent at all on every line end
                    break;
                default:
                    pushDedents = false;
                    break;
            }
            //unindent all buffered indents; 
            if (pushDedents) {
                PushDedents(0);
            }
            //now push Eos token - it will be popped first, then dedents, then EOF token
            if (pushEos) {
                var eosLocation = ComputeEosLocation();
                PushOutlineToken(_grammar.Eos, eosLocation);
            }
        }

        private void PushDedents(int untilPosition) {
            while (Indents.Peek() > untilPosition) {
                Indents.Pop();
                PushOutlineToken(_grammar.Dedent, CurrentToken.Location);
            }
        }

        private SourceLocation ComputeEosLocation() {
            if (PreviousToken == null) {
                return new SourceLocation();
            }
            //Return position at the end of previous token
            var loc = PreviousToken.Location;
            var len = PreviousToken.Length;
            return new SourceLocation(loc.Position + len, loc.Line, loc.Column + len);
        }

        private void PushOutlineToken(Terminal term, SourceLocation location) {
            OutputTokens.Push(new Token(term, location, string.Empty, null));
        }

        private readonly Grammar _grammar;
        private ParsingContext _context;
        private readonly bool _produceIndents;
        private readonly bool _checkBraces;
        private readonly bool _checkOperator;

        private bool _isContinuation;
        private bool _prevIsContinuation;
        private bool _isOperator;
        private bool _prevIsOperator;
        private bool _doubleEof;

    }

}
