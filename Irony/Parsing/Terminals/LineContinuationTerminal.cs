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
using System.Linq;
using Irony.Utilities;

namespace Irony.Parsing {

    public sealed class LineContinuationTerminal : Terminal {

        public LineContinuationTerminal(string name, params string[] startSymbols)
            : base(name, TokenCategory.Outline) {
            var symbols = startSymbols.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            StartSymbols = new StringList(symbols);
            if (StartSymbols.Count == 0) {
                StartSymbols.AddRange(DefaultStartSymbols);
            }

            Priority = TerminalPriority.High;
        }

        public StringList StartSymbols { get; }

        public CharHashSet LineTerminators { get; } = new CharHashSet { '\n', '\r', '\v' };

        #region overrides
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);

            // initialize string of start characters for fast lookup
            _startSymbolsFirsts = new string(StartSymbols.Select(s => s.First()).ToArray());

            if (EditorInfo == null) {
                EditorInfo = new TokenEditorInfo(TokenType.Delimiter, TokenColor.Comment, TokenTriggers.None);
            }
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            // Quick check
            var lookAhead = source.PreviewChar;
            var startIndex = _startSymbolsFirsts.IndexOf(lookAhead);
            if (startIndex < 0) {
                return null;
            }

            // Match start symbols
            if (!BeginMatch(source, startIndex, lookAhead)) {
                return null;
            }

            // Match NewLine
            var result = CompleteMatch(source);
            if (result != null) {
                return result;
            }

            // Report an error
            return context.CreateErrorToken(Resources.ErrNewLineExpected);
        }

        private bool BeginMatch(ISourceStream source, int startFrom, char lookAhead) {
            foreach (var startSymbol in StartSymbols.Skip(startFrom)) {
                if (startSymbol[0] != lookAhead) {
                    continue;
                }

                if (source.MatchSymbol(startSymbol)) {
                    source.PreviewPosition += startSymbol.Length;
                    return true;
                }
            }
            return false;
        }

        private Token CompleteMatch(ISourceStream source) {
            if (source.EOF) {
                return null;
            }

            do {
                // Match NewLine
                var lookAhead = source.PreviewChar;
                if (LineTerminators.Contains(lookAhead)) {
                    source.PreviewPosition++;
                    // Treat \r\n as single NewLine
                    if (!source.EOF && lookAhead == '\r' && source.PreviewChar == '\n') {
                        source.PreviewPosition++;
                    }

                    break;
                }

                // Eat up whitespace
                if (Grammar.IsWhitespaceOrDelimiter(lookAhead)) {
                    source.PreviewPosition++;
                    continue;
                }

                // Fail on anything else
                return null;
            }
            while (!source.EOF);

            // Create output token
            return source.CreateToken(OutputTerminal);
        }

        public override IList<string> GetFirsts() {
            return StartSymbols;
        }
        #endregion

        private static readonly string[] DefaultStartSymbols = { "\\", "_" };

        private string _startSymbolsFirsts = string.Concat(DefaultStartSymbols);

    }

}
