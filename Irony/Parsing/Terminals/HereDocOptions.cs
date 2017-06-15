using System;

namespace Irony.Parsing {

    [Flags]
    public enum HereDocOptions {

        None = 0,

        AllowIndentedEndToken = 0x1,
        // Behaves like PHP heredoc templates.
        IsTemplate = 0x2,
        RemoveBeginningNewLine = 0x4,

        NoEscapes = 0x10,
        AllowsUEscapes = 0x20,
        AllowsXEscapes = 0x40,
        AllowsOctalEscapes = 0x80,
        AllowsAllEscapes = AllowsUEscapes | AllowsXEscapes | AllowsOctalEscapes,

    }
}
