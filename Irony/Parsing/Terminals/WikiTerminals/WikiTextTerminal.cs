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

namespace Irony.Parsing.WikiTerminals {

    //Handles plain text
    public sealed class WikiTextTerminal : WikiTerminalBase {

        public WikiTextTerminal(string name) : base(name, WikiTermType.Text, string.Empty, string.Empty, string.Empty) {
            Priority = TerminalPriority.Low;
        }

        public static readonly char NoEscape = '\0';

        public char EscapeChar { get; } = NoEscape;

        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            var stopCharSet = new CharHashSet();
            foreach (var term in grammarData.Terminals) {
                var firsts = term.GetFirsts();
                if (firsts == null) continue;
                foreach (var first in firsts) {
                    if (!string.IsNullOrEmpty(first)) {
                        stopCharSet.Add(first[0]);
                    }
                }
            }
            if (EscapeChar != NoEscape) {
                stopCharSet.Add(EscapeChar);
            }

            _stopChars = stopCharSet.ToArray();
        }

        //override to WikiTerminalBase's method to return null, indicating there are no firsts, so it is a fallback terminal
        public override IList<string> GetFirsts() {
            return null;
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            var isEscape = source.PreviewChar == EscapeChar && EscapeChar != NoEscape;
            if (isEscape) {
                //return a token containing only escaped char
                var value = source.NextPreviewChar.ToString();
                source.PreviewPosition += 2;
                return source.CreateToken(OutputTerminal, value);
            }

            var stopIndex = source.Text.IndexOfAny(_stopChars, source.Location.Position + 1);
            if (stopIndex == source.Location.Position) {
                return null;
            }

            if (stopIndex < 0) {
                stopIndex = source.Text.Length;
            }

            source.PreviewPosition = stopIndex;
            return source.CreateToken(OutputTerminal);
        }

        private char[] _stopChars;

    }

}
