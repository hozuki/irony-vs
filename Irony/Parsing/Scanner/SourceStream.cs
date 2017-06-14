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
using System.Diagnostics;

namespace Irony.Parsing {

    public class SourceStream : ISourceStream {

        public SourceStream(string text, bool caseSensitive, int tabWidth)
            : this(text, caseSensitive, tabWidth, new SourceLocation()) {
        }

        public SourceStream(string text, bool caseSensitive, int tabWidth, SourceLocation initialLocation) {
            Text = text;
            _textLength = text.Length;
            _chars = text.ToCharArray();
            _stringComparison = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
            _tabWidth = tabWidth;
            _location = initialLocation;
            PreviewPosition = _location.Position;
            if (_tabWidth <= 1) {
                _tabWidth = 4;
            }
        }

        #region ISourceStream Members
        public string Text { get; }

        public int Position {
            get => _location.Position;
            set {
                if (_location.Position != value) {
                    SetNewPosition(value);
                }
            }
        }

        public SourceLocation Location {
            [DebuggerStepThrough]
            get => _location;
            set => _location = value;
        }

        public int PreviewPosition { get; set; }

        public char PreviewChar {
            [DebuggerStepThrough]
            get => PreviewPosition >= _textLength ? '\0' : _chars[PreviewPosition];
        }

        public char NextPreviewChar {
            [DebuggerStepThrough]
            get => PreviewPosition + 1 >= _textLength ? '\0' : _chars[PreviewPosition + 1];
        }

        public bool MatchSymbol(string symbol) {
            try {
                var cmp = string.Compare(Text, PreviewPosition, symbol, 0, symbol.Length, _stringComparison);
                return cmp == 0;
            } catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is ArgumentException) {
                //exception may be thrown if Position + symbol.length > text.Length; 
                // this happens not often, only at the very end of the file, so we don't check this explicitly
                //but simply catch the exception and return false. Again, try/catch block has no overhead
                // if exception is not thrown. 
                return false;
            }
        }

        public Token CreateToken(Terminal terminal) {
            var tokenText = GetPreviewText();
            return new Token(terminal, Location, tokenText, tokenText);
        }

        public Token CreateToken(Terminal terminal, object value) {
            var tokenText = GetPreviewText();
            return new Token(terminal, Location, tokenText, value);
        }

        public bool EOF {
            [DebuggerStepThrough]
            get => PreviewPosition >= _textLength;
        }

        #endregion

        //returns substring from Location.Position till (PreviewPosition - 1)
        private string GetPreviewText() {
            var until = PreviewPosition;
            if (until > _textLength) {
                until = _textLength;
            }
            var p = _location.Position;
            var text = Text.Substring(p, until - p);
            return text;
        }

        // To make debugging easier: show 20 chars from current position
        public override string ToString() {
            string result;
            try {
                var p = Location.Position;
                if (p + 20 < _textLength) {
                    result = Text.Substring(p, 20) + Resources.LabelSrcHaveMore;// " ..."
                } else {
                    result = Text.Substring(p) + Resources.LabelEofMark; //"(EOF)"
                }
            } catch (ArgumentOutOfRangeException) {
                result = PreviewChar + Resources.LabelSrcHaveMore;
            }
            return string.Format(Resources.MsgSrcPosToString, result, Location); //"[{0}], at {1}"
        }

        //Computes the Location info (line, col) for a new source position.
        private void SetNewPosition(int newPosition) {
            if (newPosition < Position) {
                throw new ArgumentOutOfRangeException(nameof(newPosition), Resources.ErrCannotMoveBackInSource);
            }

            var p = Position;
            var col = Location.Column;
            var line = Location.Line;
            while (p < newPosition) {
                if (p >= _textLength) {
                    break;
                }
                var curr = _chars[p];
                switch (curr) {
                    case '\n':
                        line++;
                        col = 0;
                        break;
                    case '\r':
                        break;
                    case '\t':
                        col = (col / _tabWidth + 1) * _tabWidth;
                        break;
                    default:
                        col++;
                        break;
                }
                p++;
            }
            Location = new SourceLocation(p, line, col);
        }

        private readonly StringComparison _stringComparison;
        private readonly int _tabWidth;
        private readonly char[] _chars;
        private readonly int _textLength;

        private SourceLocation _location;

    }

}
