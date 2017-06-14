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

using Irony.Parsing;

namespace Irony.Utilities {

    public static class ParsingEnumExtensions {

        public static bool IsSet(this TermFlags flags, TermFlags flag) {
            return (flags & flag) != 0;
        }

        public static bool IsSet(this LanguageFlags flags, LanguageFlags flag) {
            return (flags & flag) != 0;
        }

        public static bool IsSet(this ParseOptions options, ParseOptions option) {
            return (options & option) != 0;
        }

        public static bool IsSet(this TermListOptions options, TermListOptions option) {
            return (options & option) != 0;
        }

        public static bool IsSet(this ProductionFlags flags, ProductionFlags flag) {
            return (flags & flag) != 0;
        }

        public static bool IsSet(this ParserStateFlags flags, ParserStateFlags flag) {
            return (flags & flag) != 0;
        }

    }

}
