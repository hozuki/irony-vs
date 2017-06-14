using System;
using Irony.Utilities;

namespace Irony.Parsing {

    public sealed class HereDocTerminal : Terminal {

        public HereDocTerminal(string name, string beginLiteral)
            : this(name, beginLiteral, HereDocOptions.None) {
        }

        public HereDocTerminal(string name, string beginLiteral, HereDocOptions options)
            : base(name) {
            BeginLiteral = beginLiteral;
            Options = options;
        }

        public HereDocOptions Options { get; }

        public string BeginLiteral { get; }

        public StringSet EndLiterals { get; } = new StringSet();

        public void AddSubType(string endLiteral, HereDocOptions options) {
            throw new NotImplementedException();
        }

    }

}
