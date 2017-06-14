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

    // ScannerData is a container for all detailed info needed by scanner to read input. 
    public sealed class ScannerData {

        public ScannerData(LanguageData language) {
            Language = language;
        }

        public LanguageData Language { get; }

        // Hash table for fast terminal lookup by input char
        public TerminalLookupTable TerminalsLookup { get; } = new TerminalLookupTable();

        public TerminalList MultilineTerminals { get; } = new TerminalList();

        // Terminals with no limited set of prefixes, copied from GrammarData 
        public TerminalList NoPrefixTerminals { get; } = new TerminalList();

        // Hash table for fast lookup of non-grammar terminals by input char
        public TerminalLookupTable NonGrammarTerminalsLookup { get; } = new TerminalLookupTable();

    }

}
