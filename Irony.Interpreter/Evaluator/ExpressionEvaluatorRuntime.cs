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
using System.Linq;
using Irony.Parsing;

namespace Irony.Interpreter.Evaluator {

    public sealed class ExpressionEvaluatorRuntime : LanguageRuntime {

        public ExpressionEvaluatorRuntime(LanguageData language)
            : base(language) {
        }

        public override void Initialize() {
            base.Initialize();
            //add built-in methods, special form IIF, import Math and Environment methods
            BuiltIns.AddMethod(BuiltInPrintMethod, "print");
            BuiltIns.AddMethod(BuiltInFormatMethod, "format");
            BuiltIns.AddSpecialForm(SpecialFormsLibrary.IIf, "iif", 3, 3);
            BuiltIns.ImportStaticMembers(typeof(Math));
            BuiltIns.ImportStaticMembers(typeof(Environment));
        }

        //Built-in methods
        private static object BuiltInPrintMethod(ScriptThread thread, object[] args) {
            var text = string.Empty;
            switch (args.Length) {
                case 1:
                    text = string.Empty + args[0]; //compact and safe conversion ToString()
                    break;
                case 0:
                    break;
                default:
                    text = string.Join(" ", args);
                    break;
            }
            thread.App.WriteLine(text);
            return null;
        }

        private object BuiltInFormatMethod(ScriptThread thread, object[] args) {
            if (args == null || args.Length == 0) {
                return null;
            }
            var template = args[0] as string;
            if (template == null) {
                ThrowScriptError("Format template must be a string.");
            }
            if (args.Length == 1) {
                return template;
            }
            //create formatting args array
            var formatArgs = args.Skip(1).ToArray();
            var text = string.Format(template, formatArgs);
            return text;
        }

    }

}
