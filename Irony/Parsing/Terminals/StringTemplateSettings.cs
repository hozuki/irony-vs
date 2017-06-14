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

    //Container for settings of tempate string parser, to interpet strings having embedded values or expressions
    // like in Ruby:
    // "Hello, #{name}"
    // Default values match settings for Ruby strings
    public sealed class StringTemplateSettings {

        public string StartTag { get; set; } = "#{";

        public string EndTag { get; set; } = "}";

        public NonTerminal ExpressionRoot { get; set; }

    }

}
