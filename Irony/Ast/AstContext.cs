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
using Irony.Utilities;

namespace Irony.Ast {
    public class AstContext {

        public AstContext(LanguageData language) {
            Language = language;
        }

        public LanguageData Language { get; }

        public LogMessageList Messages { get; internal set; }

        public Type DefaultNodeType { get; set; }

        /// <summary>
        /// Default node type for literals.
        /// </summary>
        public Type DefaultLiteralNodeType { get; set; }

        /// <summary>
        /// Default node type for identifiers.
        /// </summary>
        public Type DefaultIdentifierNodeType { get; set; }

        public void AddMessage(ErrorLevel level, SourceLocation location, string message, params object[] args) {
            if (args != null && args.Length > 0) {
                message = string.Format(message, args);
            }
            Messages.Add(new LogMessage(level, location, message, null));
        }

    }
}