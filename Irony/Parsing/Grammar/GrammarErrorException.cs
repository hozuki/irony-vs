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

    //Used to cancel parser construction when fatal error is found
    public class GrammarErrorException : Exception {

        public GrammarErrorException(GrammarError error)
            : base(string.Empty) {
            Error = error;
        }

        public GrammarErrorException(string message, GrammarError error)
            : base(message) {
            Error = error;
        }

        public GrammarError Error { get; }

    }

}
