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

    // GrammarData is a container for all basic info about the grammar
    // GrammarData is a field in LanguageData object. 
    public sealed class GrammarData {

        public GrammarData(LanguageData language) {
            Language = language;
            Grammar = language.Grammar;
        }

        public LanguageData Language { get; }

        public Grammar Grammar { get; }

        public NonTerminal AugmentedRoot { get; internal set; }

        public NonTerminalSet AugmentedSnippetRoots { get; } = new NonTerminalSet();

        public BnfTermSet AllTerms { get; } = new BnfTermSet();

        public TerminalSet Terminals { get; } = new TerminalSet();

        public NonTerminalSet NonTerminals { get; } = new NonTerminalSet();

        // Terminals that have no limited set of prefixes.
        public TerminalSet NoPrefixTerminals { get; } = new TerminalSet();

    }

}
