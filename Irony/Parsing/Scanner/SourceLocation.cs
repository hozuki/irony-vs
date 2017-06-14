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

    public struct SourceLocation : IComparable<SourceLocation> {

        public SourceLocation(int position, int line, int column) {
            Position = position;
            Line = line;
            Column = column;
        }

        public int Position { get; }

        /// <summary>
        /// Source line number, 0-based.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Source column number, 0-based.
        /// </summary>
        public int Column { get; }

        //Line/col are zero-based internally
        public override string ToString() {
            return string.Format(Resources.FmtRowCol, Line + 1, Column + 1);
        }

        //Line and Column displayed to user should be 1-based
        public string ToUiString() {
            return string.Format(Resources.FmtRowCol, Line + 1, Column + 1);
        }

        public static int Compare(SourceLocation x, SourceLocation y) {
            return x.Position.CompareTo(y.Position);
        }

        public int CompareTo(SourceLocation other) {
            return Compare(this, other);
        }

        public static SourceLocation Empty { get; } = new SourceLocation();

        public static SourceLocation operator +(SourceLocation x, SourceLocation y) {
            return new SourceLocation(x.Position + y.Position, x.Line + y.Line, x.Column + y.Column);
        }

        public static SourceLocation operator +(SourceLocation x, int offsetInLine) {
            return new SourceLocation(x.Position + offsetInLine, x.Line, x.Column + offsetInLine);
        }

    }

}
