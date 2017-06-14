using System;

namespace Irony.Parsing {
    [Flags]
    public enum ParserStateFlags {

        None = 0,
        // A flag to mark a state for setting implied precedence.
        ImpliedPrecedence = 0x01000000

    }
}
