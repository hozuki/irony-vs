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
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Irony.Parsing {

    public sealed class GrammarErrorList : List<GrammarError> {

        public void Add(GrammarErrorLevel level, ParserState state, string message, params object[] args) {
            if (args != null && args.Length > 0)
                message = String.Format(message, args);
            Add(new GrammarError(level, state, message));
        }

        [ContractAnnotation("=> halt")]
        public void AddAndThrow(GrammarErrorLevel level, ParserState state, string message, params object[] args) {
            Add(level, state, message, args);
            var error = this[this.Count - 1];
            var exc = new GrammarErrorException(error.Message, error);
            throw exc;
        }

        public GrammarErrorLevel GetMaxLevel() {
            var max = GrammarErrorLevel.NoError;
            foreach (var err in this) {
                if (max < err.Level) {
                    max = err.Level;
                }
            }
            return max;
        }

    }

}
