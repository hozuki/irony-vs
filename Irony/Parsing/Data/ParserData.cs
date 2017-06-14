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

using Irony.Parsing.ParserActions;

namespace Irony.Parsing {
    public sealed class ParserData {

        public ParserData(LanguageData language) {
            Language = language;
        }

        public LanguageData Language { get; }

        //main initial state
        public ParserState InitialState { get; internal set; }

        // Lookup table: AugmRoot => InitialState
        public ParserStateTable InitialStates { get; } = new ParserStateTable();

        public ParserStateList States { get; } = new ParserStateList();

        public ParserAction ErrorAction { get; internal set; }

    }
}
