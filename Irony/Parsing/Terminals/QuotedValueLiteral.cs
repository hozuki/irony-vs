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

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Irony.Parsing {

    //Terminal for reading values enclosed in a pair of start/end characters. For ex, date literal #15/10/2009# in VB
    public sealed class QuotedValueLiteral : DataLiteralBase {

        public QuotedValueLiteral(string name, string startEndSymbol, TypeCode dataType)
            : this(name, startEndSymbol, startEndSymbol, dataType) {
        }

        public QuotedValueLiteral(string name, string startSymbol, string endSymbol, TypeCode dataType)
            : base(name, dataType) {
            StartSymbol = startSymbol;
            EndSymbol = endSymbol;
        }

        [NotNull]
        public string StartSymbol { get; set; }

        [NotNull]
        public string EndSymbol { get; set; }

        public override IList<string> GetFirsts() {
            return new[] { StartSymbol };
        }

        protected override string ReadBody(ParsingContext context, ISourceStream source) {
            if (!source.MatchSymbol(StartSymbol)) {
                return null; //this will result in null returned from TryMatch, no token
            }

            var start = source.Location.Position + StartSymbol.Length;
            var end = source.Text.IndexOf(EndSymbol, start, StringComparison.InvariantCulture);
            if (end < 0) {
                return null;
            }

            var body = source.Text.Substring(start, end - start);
            source.PreviewPosition = end + EndSymbol.Length; //move beyond the end of EndSymbol
            return body;
        }

    }

}
