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

namespace Irony.Parsing.WikiTerminals {

    public sealed class WikiBlockTerminal : WikiTerminalBase {

        public WikiBlockTerminal(string name, WikiBlockType blockType, string openTag, string closeTag, string htmlElementName)
            : base(name, WikiTermType.Block, openTag, closeTag, htmlElementName) {
            BlockType = blockType;
        }

        public WikiBlockType BlockType { get; }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            if (!source.MatchSymbol(OpenTag)) return null;
            source.PreviewPosition += OpenTag.Length;
            var endPos = source.Text.IndexOf(CloseTag, source.PreviewPosition, StringComparison.InvariantCulture);
            string content;
            if (endPos > 0) {
                content = source.Text.Substring(source.PreviewPosition, endPos - source.PreviewPosition);
                source.PreviewPosition = endPos + CloseTag.Length;
            } else {
                content = source.Text.Substring(source.PreviewPosition, source.Text.Length - source.PreviewPosition);
                source.PreviewPosition = source.Text.Length;
            }
            var token = source.CreateToken(OutputTerminal, content);
            return token;
        }

    }

}
