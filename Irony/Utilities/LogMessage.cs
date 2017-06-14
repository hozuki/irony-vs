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
using Irony.Parsing;

namespace Irony.Utilities {

    //Container for syntax errors and warnings
    public sealed class LogMessage : IComparable<LogMessage> {

        public LogMessage(ErrorLevel level, SourceLocation location, string message, ParserState parserState) {
            Level = level;
            Location = location;
            Message = message;
            ParserState = parserState;
        }

        public ErrorLevel Level { get; }

        public ParserState ParserState { get; }

        public SourceLocation Location { get; }

        public string Message { get; }

        public static int CompareByLocation(LogMessage x, LogMessage y) {
            return SourceLocation.Compare(x.Location, y.Location);
        }

        public int CompareTo(LogMessage other) {
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
