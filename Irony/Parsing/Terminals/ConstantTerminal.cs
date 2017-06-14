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

namespace Irony.Parsing {

    public sealed class ConstantTerminal : Terminal {

        public ConstantTerminal(string name, Type nodeType = null) : base(name) {
            SetFlag(TermFlags.IsConstant);
            if (nodeType != null) {
                AstConfig.NodeType = nodeType;
            }

            Priority = TerminalPriority.High; //constants have priority over normal identifiers
        }

        public readonly ConstantsTable Constants = new ConstantsTable();

        public void Add(string lexeme, object value) {
            Constants[lexeme] = value;
        }

        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            if (EditorInfo == null) {
                EditorInfo = new TokenEditorInfo(TokenType.Unknown, TokenColor.Text, TokenTriggers.None);
            }
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            var text = source.Text;
            foreach (var entry in Constants) {
                source.PreviewPosition = source.Position;
                var constant = entry.Key;
                if (source.PreviewPosition + constant.Length > text.Length) {
                    continue;
                }

                if (source.MatchSymbol(constant)) {
                    source.PreviewPosition += constant.Length;
                    if (!Grammar.IsWhitespaceOrDelimiter(source.PreviewChar)) {
                        continue; //make sure it is delimiter
                    }

                    return source.CreateToken(OutputTerminal, entry.Value);
                }
            }
            return null;
        }

        public override IList<string> GetFirsts() {
            var array = new string[Constants.Count];
            Constants.Keys.CopyTo(array, 0);
            return array;
        }

    }

}
