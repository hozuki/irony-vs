﻿#region License
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
using System.Text;
using Irony.Utilities;

// Sometimes language definition includes tokens that have no specific format, but are just "all text until some terminator character(s)";
// FreeTextTerminal allows easy implementation of such language element.

namespace Irony.Parsing {

    public class FreeTextLiteral : Terminal {

        public FreeTextLiteral(string name, params string[] terminators)
            : this(name, FreeTextOptions.None, terminators) {
        }

        public FreeTextLiteral(string name, FreeTextOptions freeTextOptions, params string[] terminators)
            : base(name) {
            FreeTextOptions = freeTextOptions;
            Terminators.UnionWith(terminators);
            SetFlag(TermFlags.IsLiteral);
        }

        public StringSet Terminators { get; } = new StringSet();

        public StringSet Firsts { get; } = new StringSet();

        public StringDictionary Escapes { get; } = new StringDictionary();

        public FreeTextOptions FreeTextOptions { get; }

        public override IList<string> GetFirsts() {
            var result = new StringList();
            result.AddRange(Firsts);
            return result;
        }

        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            _isSimple = Terminators.Count == 1 && Escapes.Count == 0;
            if (_isSimple) {
                _singleTerminator = Terminators.First();
                return;
            }
            var stopChars = new CharHashSet();
            foreach (var key in Escapes.Keys) {
                stopChars.Add(key[0]);
            }

            foreach (var t in Terminators) {
                stopChars.Add(t[0]);
            }

            _stopChars = stopChars.ToArray();
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            if (!TryMatchPrefixes(context, source)) {
                return null;
            }

            return _isSimple ? TryMatchContentSimple(context, source) : TryMatchContentExtended(context, source);
        }

        private bool TryMatchPrefixes(ParsingContext context, ISourceStream source) {
            if (Firsts.Count == 0) {
                return true;
            }

            foreach (var first in Firsts) {
                if (source.MatchSymbol(first)) {
                    source.PreviewPosition += first.Length;
                    return true;
                }
            }

            return false;
        }

        private Token TryMatchContentSimple(ParsingContext context, ISourceStream source) {
            var startPos = source.PreviewPosition;
            var termLen = _singleTerminator.Length;
            var stringComp = Grammar.CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
            var termPos = source.Text.IndexOf(_singleTerminator, startPos, stringComp);
            if (termPos < 0 && IsSet(FreeTextOptions.AllowEof)) {
                termPos = source.Text.Length;
            }

            if (termPos < 0) {
                return context.CreateErrorToken(Resources.ErrFreeTextNoEndTag, _singleTerminator);
            }

            var textEnd = termPos;
            if (IsSet(FreeTextOptions.IncludeTerminator)) {
                textEnd += termLen;
            }

            var tokenText = source.Text.Substring(startPos, textEnd - startPos);
            if (string.IsNullOrEmpty(tokenText) && (FreeTextOptions & FreeTextOptions.AllowEmpty) == 0) {
                return null;
            }
            // The following line is a fix submitted by user rmcase
            source.PreviewPosition = IsSet(FreeTextOptions.ConsumeTerminator) ? termPos + termLen : termPos;
            return source.CreateToken(OutputTerminal, tokenText);
        }

        private Token TryMatchContentExtended(ParsingContext context, ISourceStream source) {
            var tokenText = new StringBuilder();
            while (true) {
                //Find next position of one of stop chars
                var nextPos = source.Text.IndexOfAny(_stopChars, source.PreviewPosition);
                if (nextPos == -1) {
                    if (IsSet(FreeTextOptions.AllowEof)) {
                        source.PreviewPosition = source.Text.Length;
                        return source.CreateToken(OutputTerminal);
                    }
                    return null;
                }
                var newText = source.Text.Substring(source.PreviewPosition, nextPos - source.PreviewPosition);
                tokenText.Append(newText);
                source.PreviewPosition = nextPos;
                //if it is escape, add escaped text and continue search
                if (CheckEscape(source, tokenText)) {
                    continue;
                }
                //check terminators
                if (CheckTerminators(source, tokenText)) {
                    break; //from while (true); we reached 
                }
                //The current stop is not at escape or terminator; add this char to token text and move on 
                tokenText.Append(source.PreviewChar);
                source.PreviewPosition++;
            }
            var text = tokenText.ToString();
            if (string.IsNullOrEmpty(text) && (FreeTextOptions & FreeTextOptions.AllowEmpty) == 0) {
                return null;
            }

            return source.CreateToken(OutputTerminal, text);
        }

        private bool CheckEscape(ISourceStream source, StringBuilder tokenText) {
            foreach (var dictEntry in Escapes) {
                if (source.MatchSymbol(dictEntry.Key)) {
                    source.PreviewPosition += dictEntry.Key.Length;
                    tokenText.Append(dictEntry.Value);
                    return true;
                }
            }
            return false;
        }

        private bool CheckTerminators(ISourceStream source, StringBuilder tokenText) {
            foreach (var term in Terminators) {
                if (!source.MatchSymbol(term)) {
                    continue;
                }

                if (IsSet(FreeTextOptions.IncludeTerminator)) {
                    tokenText.Append(term);
                }

                if (IsSet(FreeTextOptions.ConsumeTerminator | FreeTextOptions.IncludeTerminator)) {
                    source.PreviewPosition += term.Length;
                }

                return true;
            }

            return false;
        }

        private bool IsSet(FreeTextOptions option) {
            return (FreeTextOptions & option) != 0;
        }

        private char[] _stopChars;

        //True if we have a single Terminator and no escapes
        private bool _isSimple;

        private string _singleTerminator;

    }

}
