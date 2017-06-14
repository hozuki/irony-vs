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

using Irony.Parsing.ParserActions;
using Irony.Utilities;

namespace Irony.Parsing {

    public sealed class ParserState {

        public ParserState(string name) {
            Name = name;
        }

        public string Name { get; }

        public ParserActionTable Actions { get; } = new ParserActionTable();

        //Defined for states with a single reduce item; Parser.GetAction returns this action if it is not null.
        public ParserAction DefaultAction { get; internal set; }

        //Expected terms contains terminals is to be used in 
        //Parser-advise-to-Scanner facility would use it to filter current terminals when Scanner has more than one terminal for current char,
        //   it can ask Parser to filter the list using the ExpectedTerminals in current Parser state. 
        public TerminalSet ExpectedTerminals { get; } = new TerminalSet();

        //Used for error reporting, we would use it to include list of expected terms in error message 
        // It is reduced compared to ExpectedTerms - some terms are "merged" into other non-terminals (with non-empty DisplayName)
        //   to make message shorter and cleaner. It is computed on-demand in CoreParser
        public StringSet ReportedExpectedSet { get; internal set; }

        //transient, used only during automaton construction and may be cleared after that
        internal ParserStateData BuilderData { get; set; }

        //Custom flags available for use by language/parser authors, to "mark" states in some way
        // Irony reserves the highest order byte for internal use
        public ParserStateFlags CustomFlags { get; internal set; }

        public void ClearData() {
            BuilderData = null;
        }

        public override string ToString() {
            return Name;
        }

        public override int GetHashCode() {
            return Name.GetHashCode();
        }

    }

}
