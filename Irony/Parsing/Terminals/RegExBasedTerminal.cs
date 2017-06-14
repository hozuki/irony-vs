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
using System.Text.RegularExpressions;
using Irony.Utilities;

namespace Irony.Parsing {

    //Note: this class was not tested at all
    // Based on contributions by CodePlex user sakana280
    // 12.09.2008 - breaking change! added "name" parameter to the constructor
    public sealed class RegexBasedTerminal : Terminal {

        public RegexBasedTerminal(string pattern, params string[] prefixes)
            : base("name") {
            Pattern = pattern;
            if (prefixes != null) {
                Prefixes.AddRange(prefixes);
            }
        }

        public RegexBasedTerminal(string name, string pattern, params string[] prefixes)
            : base(name) {
            Pattern = pattern;
            if (prefixes != null) {
                Prefixes.AddRange(prefixes);
            }
        }

        #region public properties
        public string Pattern { get; }

        public StringList Prefixes { get; } = new StringList();

        public Regex Expression => _expression;
        #endregion

        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            var workPattern = @"\G(" + Pattern + ")";
            var options = (Grammar.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            _expression = new Regex(workPattern, options);
            if (EditorInfo == null) {
                EditorInfo = new TokenEditorInfo(TokenType.Unknown, TokenColor.Text, TokenTriggers.None);
            }
        }

        public override IList<string> GetFirsts() {
            return Prefixes;
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            var m = _expression.Match(source.Text, source.PreviewPosition);
            if (!m.Success || m.Index != source.PreviewPosition) {
                return null;
            }

            source.PreviewPosition += m.Length;
            return source.CreateToken(OutputTerminal);
        }

        private Regex _expression;

    }

}
