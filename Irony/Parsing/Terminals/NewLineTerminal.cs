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

    //This is a simple NewLine terminal recognizing line terminators for use in grammars for line-based languages like VB
    // instead of more complex alternative of using CodeOutlineFilter. 
    public sealed class NewLineTerminal : Terminal {

        public NewLineTerminal(string name)
            : base(name, TokenCategory.Outline) {
            ErrorAlias = Resources.LabelLineBreak;  // "[line break]";
            Flags |= TermFlags.IsPunctuation;
        }

        public CharHashSet LineTerminators { get; } = new CharHashSet { '\n', '\r', '\v' };

        #region overrides: Init, GetFirsts, TryMatch
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            Grammar.UsesNewLine = true; //That will prevent SkipWhitespace method from skipping new-line chars
        }

        public override IList<string> GetFirsts() {
            var firsts = new StringList();
            firsts.AddRange(LineTerminators.Select(t => t.ToString()));
            return firsts;
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            var current = source.PreviewChar;
            if (!LineTerminators.Contains(current)) {
                return null;
            }
            //Treat \r\n as a single terminator
            var doExtraShift = (current == '\r' && source.NextPreviewChar == '\n');
            source.PreviewPosition++; //main shift
            if (doExtraShift) {
                source.PreviewPosition++;
            }

            var result = source.CreateToken(OutputTerminal);
            return result;
        }
        #endregion

    }

}
