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
using System.Threading;
using Irony.Parsing;

namespace Irony.Interpreter {

    //WARNING: Ctrl-C for aborting running script does NOT work when you run console app from Visual Studio 2010. 
    // Run executable directly from bin folder. 
    public sealed class CommandLine {

        public CommandLine(LanguageRuntime runtime)
            : this(runtime, null) {
        }

        public CommandLine(LanguageRuntime runtime, IConsoleAdapter adapter) {
            Runtime = runtime;
            Adapter = adapter ?? new ConsoleAdapter();
            var grammar = runtime.Language.Grammar;
            Title = grammar.ConsoleTitle;
            Greeting = grammar.ConsoleGreeting;
            Prompt = grammar.ConsolePrompt;
            PromptMoreInput = grammar.ConsolePromptMoreInput;
            App = new ScriptApp(Runtime) {
                ParserMode = ParseMode.CommandLine,
                RethrowExceptions = false
            };
            // App.PrintParseErrors = false;
        }

        public LanguageRuntime Runtime { get; }

        public IConsoleAdapter Adapter { get; }

        //Initialized from grammar
        public string Title { get; }

        public string Greeting { get; }

        //default prompt
        public string Prompt { get; }

        //prompt to show when more input is expected
        public string PromptMoreInput { get; }

        public ScriptApp App { get; }

        public bool IsEvaluating { get; private set; }

        public void Run() {
            try {
                RunImpl();
            } catch (Exception ex) {
                Adapter.SetTextStyle(ConsoleTextStyle.Error);
                Adapter.WriteLine(Resources.ErrConsoleFatalError);
                Adapter.WriteLine(ex.ToString());
                Adapter.SetTextStyle(ConsoleTextStyle.Normal);
                Adapter.WriteLine(Resources.MsgPressAnyKeyToExit);
                Adapter.Read();
            }
        }

        private void RunImpl() {
            Adapter.SetTitle(Title);
            Adapter.WriteLine(Greeting);
            while (true) {
                Adapter.Canceled = false;
                Adapter.SetTextStyle(ConsoleTextStyle.Normal);
                var prompt = (App.Status == AppStatus.WaitingMoreInput ? PromptMoreInput : Prompt);

                //Write prompt, read input, check for Ctrl-C
                Adapter.Write(prompt);
                var input = Adapter.ReadLine();
                if (Adapter.Canceled) {
                    if (Confirm(Resources.MsgExitConsoleYN)) {
                        return;
                    } else {
                        continue; //from the start of the loop
                    }
                }

                //Execute
                App.ClearOutputBuffer();
                EvaluateAsync(input);
                //Evaluate(input);
                WaitForScriptComplete();

                switch (App.Status) {
                    case AppStatus.Ready: //success
                        Adapter.WriteLine(App.GetOutput());
                        break;
                    case AppStatus.SyntaxError:
                        Adapter.WriteLine(App.GetOutput()); //write all output we have
                        Adapter.SetTextStyle(ConsoleTextStyle.Error);
                        foreach (var err in App.GetParserMessages()) {
                            Adapter.WriteLine(string.Empty.PadRight(prompt.Length + err.Location.Column) + "^"); //show err location
                            Adapter.WriteLine(err.Message); //print message
                        }
                        break;
                    case AppStatus.Crash:
                    case AppStatus.RuntimeError:
                        ReportException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }

        private void WaitForScriptComplete() {
            Adapter.Canceled = false;
            while (true) {
                Thread.Sleep(50);
                if (!IsEvaluating) {
                    return;
                }

                if (Adapter.Canceled) {
                    Adapter.Canceled = false;
                    if (Confirm(Resources.MsgAbortScriptYN)) {
                        WorkerThreadAbort();
                    }
                }
            }
        }

        private void Evaluate(string script) {
            try {
                IsEvaluating = true;
                App.Evaluate(script);
            } finally {
                IsEvaluating = false;
            }
        }

        private void EvaluateAsync(string script) {
            IsEvaluating = true;
            _workerThread = new Thread(WorkerThreadStart);
            _workerThread.Start(script);
        }

        private void WorkerThreadStart(object data) {
            try {
                var script = data as string;
                App.Evaluate(script);
            } finally {
                IsEvaluating = false;
            }
        }

        private void WorkerThreadAbort() {
            try {
                _workerThread.Abort();
                _workerThread.Join(50);
            } finally {
                IsEvaluating = false;
            }
        }

        private bool Confirm(string message) {
            Adapter.WriteLine(string.Empty);
            Adapter.Write(message);
            var input = Adapter.ReadLine();
            return Resources.ConsoleYesChars.Contains(input);
        }

        private void ReportException() {
            Adapter.SetTextStyle(ConsoleTextStyle.Error);
            var ex = App.LastException;
            if (ex is ScriptException scriptEx) {
                Adapter.WriteLine(scriptEx.Message + " " + Resources.LabelLocation + " " + scriptEx.Location.ToUiString());
            } else {
                if (App.Status == AppStatus.Crash) {
                    //Unexpected interpreter crash:  the full stack when debugging your language  
                    Adapter.WriteLine(ex.ToString());
                } else {
                    Adapter.WriteLine(ex.Message);
                }
            }
        }

        private Thread _workerThread;

    }

}
