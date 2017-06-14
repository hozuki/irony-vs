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

using System.Collections.Generic;
using Irony.Parsing;

namespace Irony.Interpreter.Evaluator {

    public sealed class ExpressionEvaluator {

        //Default constructor, creates default evaluator 
        public ExpressionEvaluator()
            : this(new ExpressionEvaluatorGrammar()) {
        }

        //Default constructor, creates default evaluator 
        public ExpressionEvaluator(ExpressionEvaluatorGrammar grammar) {
            Grammar = grammar;
            Language = new LanguageData(Grammar);
            Parser = new Parser(Language);
            Runtime = Grammar.CreateRuntime(Language);
            App = new ScriptApp(Runtime);
        }

        public ExpressionEvaluatorGrammar Grammar { get; }

        public Parser Parser { get; }

        public LanguageData Language { get; }

        public LanguageRuntime Runtime { get; }

        public ScriptApp App { get; }

        public IDictionary<string, object> Globals => App.Globals;

        public object Evaluate(string script) {
            var result = App.Evaluate(script);
            return result;
        }

        public object Evaluate(ParseTree parsedScript) {
            var result = App.Evaluate(parsedScript);
            return result;
        }

        //Evaluates again the previously parsed/evaluated script
        public object Evaluate() {
            return App.Evaluate();
        }

        public void ClearOutput() {
            App.ClearOutputBuffer();
        }

        public string GetOutput() {
            return App.GetOutput();
        }

    }

}
