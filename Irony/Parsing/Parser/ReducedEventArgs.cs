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

    public sealed class ReducedEventArgs : ParsingEventArgs {

        public ReducedEventArgs(ParsingContext context, Production reducedProduction, ParseTreeNode resultNode)
            : base(context) {
            ReducedProduction = reducedProduction;
            ResultNode = resultNode;
        }

        public Production ReducedProduction { get; }

        public ParseTreeNode ResultNode { get; }

    }

}
