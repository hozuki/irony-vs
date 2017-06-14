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

namespace Irony.Parsing.ParserActions {

    public sealed class AcceptParserAction : ParserAction {

        public override void Execute(ParsingContext context) {
            //Pop root
            context.CurrentParseTree.Root = context.ParserStack.Pop();
            context.Status = ParserStatus.Accepted;
        }

        public override string ToString() {
            return Resources.LabelActionAccept;
        }

    }

}
