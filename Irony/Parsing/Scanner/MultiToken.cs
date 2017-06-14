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

namespace Irony.Parsing {

    //Some terminals may need to return a bunch of tokens in one call to TryMatch; MultiToken is a container for these tokens
    public class MultiToken : Token {

        public MultiToken(params Token[] tokens)
            : this(tokens[0].Terminal, tokens[0].Location, new TokenList()) {
            ChildTokens.AddRange(tokens);
        }

        public MultiToken(Terminal term, SourceLocation location, TokenList childTokens)
            : base(term, location, string.Empty, null) {
            ChildTokens = childTokens;
        }

        public TokenList ChildTokens { get; }

    }

}
