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

namespace Irony.Interpreter {

    public sealed class ScriptException : Exception {

        public ScriptException(string message)
            : base(message) {
        }

        public ScriptException(string message, Exception inner)
            : base(message, inner) {
        }

        public ScriptException(string message, Exception inner, SourceLocation location, ScriptStackTrace stack)
            : base(message, inner) {
            Location = location;
            ScriptStackTrace = stack;
        }

        public SourceLocation Location { get; internal set; }

        public ScriptStackTrace ScriptStackTrace { get; internal set; }

        public override string ToString() {
            return Message + Environment.NewLine + ScriptStackTrace.ToString();
        }

    }

}
