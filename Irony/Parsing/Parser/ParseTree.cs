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

using System.Linq;
using Irony.Utilities;

namespace Irony.Parsing {

    public sealed class ParseTree {

        public ParseTree(string sourceText, string fileName) {
            SourceText = sourceText;
            FileName = fileName;
            Status = ParseTreeStatus.Parsing;
        }

        public ParseTreeStatus Status { get; internal set; }

        public string SourceText { get; }

        public string FileName { get; }

        public TokenList Tokens { get; } = new TokenList();

        public TokenList OpenBraces { get; } = new TokenList();

        public ParseTreeNode Root { get; internal set; }

        public LogMessageList ParserMessages { get; } = new LogMessageList();

        public long ParseTimeMilliseconds { get; internal set; }

        // Custom data object, use it anyway you want.
        public object Tag { get; set; }

        public bool HasErrors => ParserMessages.Count != 0 && ParserMessages.Any(err => err.Level >= ErrorLevel.Error);

    }

}
