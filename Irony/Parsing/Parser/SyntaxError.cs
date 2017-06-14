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

namespace Irony.Parsing {

    //Container for syntax error
    public sealed class SyntaxError : IComparable<SyntaxError> {

        public SyntaxError(SourceLocation location, string message, ParserState parserState) {
            Location = location;
            Message = message;
            ParserState = parserState;
        }

        public SourceLocation Location { get; }

        public string Message { get; }

        public ParserState ParserState { get; }

        public static int CompareByLocation(SyntaxError x, SyntaxError y) {
            return SourceLocation.Compare(x.Location, y.Location);
        }

        public int CompareTo(SyntaxError other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }
            return CompareByLocation(this, other);
        }

        public override string ToString() {
            return Message;
        }

    }

}
