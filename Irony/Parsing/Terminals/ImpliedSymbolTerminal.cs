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

using Irony.Utilities;

namespace Irony.Parsing {

    //In some grammars there is a situation when some operator symbol can be skipped in source text and should be implied by parser.
    // In arithmetics, we often imply "*" operator in formulas:
    //  x y => x * y.
    // The SearchGrammar in Samples provides another example: two consequtive terms imply "and" operator and should be treated as such:
    //   x y   => x AND y 
    // We could use a simple nullable Non-terminal terminal in this case, but the problem is that we cannot associate precedence
    // and associativity with non-terminal, only with terminals. Precedence is important here because the implied symbol identifies binary
    // operation, so parser should be able to use precedence value(s) when resolving shift/reduce ambiguity. 
    // So here comes ImpliedSymbolTerminal - it is a terminal that produces a token with empty text. 
    // It relies on scanner-parser link enabled - so the implied symbol token is created ONLY 
    // when the current parser state allows it and there are no other alternatives (hence lowest priority value).
    // See SearchGrammar as an example of use of this terminal. 
    public sealed class ImpliedSymbolTerminal : Terminal {

        public ImpliedSymbolTerminal(string name)
            : base(name) {
            //This terminal should be tried after all candidate terminals failed. 
            Priority = TerminalPriority.Low;
        }

        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            //Check that Parser-scanner link is enabled - this terminal can be used only if this link is enabled
            if (Grammar.LanguageFlags.IsSet(LanguageFlags.DisableScannerParserLink)) {
                grammarData.Language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrImpliedOpUseParserLink, Name);
            }
            //"ImpliedSymbolTerminal cannot be used in grammar with DisableScannerParserLink flag set"
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            //Create an empty token representing an implied symbol.
            return source.CreateToken(this);
        }

    }

}
