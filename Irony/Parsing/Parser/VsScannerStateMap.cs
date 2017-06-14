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

using System.Runtime.InteropServices;

namespace Irony.Parsing {

    // A struct used for packing/unpacking ScannerState int value; used for VS integration.
    // When Terminal produces incomplete token, it sets 
    // this state to non-zero value; this value identifies this terminal as the one who will continue scanning when
    // it resumes, and the terminal's internal state when there may be several types of multi-line tokens for one terminal.
    // For ex., there maybe several types of string literal like in Python. 
    public sealed class VsScannerStateMap {

        internal VsScannerStateMap() {
        }

        public int Value {
            get => _internal.Value;
            set => _internal.Value = value;
        }

        public byte TerminalIndex {
            get => _internal.TerminalIndex;
            set => _internal.TerminalIndex = value;
        }

        public byte TokenSubType {
            get => _internal.TokenSubType;
            set => _internal.TokenSubType = value;
        }

        public short TerminalFlags {
            get => _internal.TerminalFlags;
            set => _internal.TerminalFlags = value;
        }

        private VsScannerStateMapInternal _internal = new VsScannerStateMapInternal();

        [StructLayout(LayoutKind.Explicit)]
        private struct VsScannerStateMapInternal {

            [FieldOffset(0)]
            public int Value;

            [FieldOffset(0)]
            public byte TerminalIndex;   //1-based index of active multiline term in MultilineTerminals

            [FieldOffset(1)]
            public byte TokenSubType;         //terminal subtype (used in StringLiteral to identify string kind)

            [FieldOffset(2)]
            public short TerminalFlags;  //Terminal flags

        }

    }

}
