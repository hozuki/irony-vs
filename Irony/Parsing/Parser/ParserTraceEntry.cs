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

namespace Irony.Parsing {

    public class ParserTraceEntry {

        public ParserTraceEntry(ParserState state, ParseTreeNode stackTop, ParseTreeNode input, string message, bool isError) {
            State = state;
            StackTop = stackTop;
            Input = input;
            Message = message;
            IsError = isError;
        }

        public ParserState State { get; }

        public ParseTreeNode StackTop { get; }

        public ParseTreeNode Input { get; }

        public string Message { get; }

        public bool IsError { get; }

    }

}
