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
using Irony.Interpreter.Ast;
using Irony.Parsing;
using JetBrains.Annotations;

namespace Irony.Interpreter {

    //Note: mark the derived language-specific class as sealed - important for JIT optimizations
    // details here: http://www.codeproject.com/KB/dotnet/JITOptimizations.aspx
    public partial class LanguageRuntime {

        public LanguageRuntime(LanguageData language) {
            Language = language;
            NoneValue = NoneClass.Value;
            BuiltIns = new BindingSourceTable(Language.Grammar.CaseSensitive);
            Initialize();
        }

        public LanguageData Language { get; }

        public OperatorHandler OperatorHandler { get; protected internal set; }

        //Converter of the result for comparison operation; converts bool value to values
        // specific for the language
        public UnaryOperatorMethod BoolResultConverter { get; protected internal set; }

        //An unassigned reserved object for a language implementation
        public NoneClass NoneValue { get; }

        //Built-in binding sources
        public BindingSourceTable BuiltIns { get; }

        public virtual void Initialize() {
            InitOperatorImplementations();
        }

        public virtual bool IsTrue(object value) {
            if (value is bool) {
                return (bool)value;
            }
            if (value is int) {
                return ((int)value != 0);
            }
            if (value == NoneValue) {
                return false;
            }
            return value != null;
        }

        [StringFormatMethod("message")]
        [ContractAnnotation("=> halt")]
        protected static void ThrowScriptError(string message, params object[] args) {
            if (args != null && args.Length > 0) {
                message = string.Format(message, args);
            }
            throw new ScriptException(message);
        }

        [StringFormatMethod("message")]
        [ContractAnnotation("=> halt")]
        private static void ThrowError(string message, params object[] args) {
            if (args != null && args.Length > 0) {
                message = string.Format(message, args);
            }
            throw new Exception(message);
        }

    }

}
