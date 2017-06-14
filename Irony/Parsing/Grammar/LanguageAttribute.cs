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
using System.Reflection;

namespace Irony.Parsing {

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LanguageAttribute : Attribute {

        public LanguageAttribute()
            : this(null) {
        }

        public LanguageAttribute(string languageName)
            : this(languageName, "1.0", string.Empty) {
        }

        public LanguageAttribute(string languageName, string version, string description) {
            LanguageName = languageName;
            Version = version;
            Description = description;
        }

        public string LanguageName { get; }

        public string Version { get; }

        public string Description { get; }

        public static LanguageAttribute GetValue(Type grammarClass) {
            var attr = grammarClass.GetCustomAttribute<LanguageAttribute>();
            return attr;
        }

    }//class

}
