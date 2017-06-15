using System;

namespace Irony.Parsing {

    [Flags]
    public enum HereDocOptions {

        None = 0,

        AllowIndentedEndToken = 0x1,
        // Behaves like PHP heredoc templates.
        IsTemplate = 0x2,
        RemoveBeginningNewLine = 0x4,
        /// <summary>
        /// Must be used in combination with <see cref="AllowIndentedEndToken"/>.
        /// </summary>
        RemoveIndents = 0x8,

        NoEscapes = 0x10,
        AllowsUEscapes = 0x20,
        AllowsXEscapes = 0x40,
        AllowsOctalEscapes = 0x80,
        AllowsAllEscapes = AllowsUEscapes | AllowsXEscapes | AllowsOctalEscapes,

    }
}
