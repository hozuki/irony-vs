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
using System.Reflection;
using System.Security;
using System.Text;
using Irony.Interpreter.Ast;
using Irony.Interpreter.Utilities;
using Irony.Parsing;
using Irony.Utilities;

namespace Irony.Interpreter {

    /// <summary> Represents a running instance of a script application.  </summary>
    public sealed class ScriptApp {

        public ScriptApp(LanguageData language) {
            Language = language;
            var grammar = language.Grammar as InterpretedLanguageGrammar;
            Runtime = grammar?.CreateRuntime(language) ?? throw new InvalidOperationException();
            DataMap = new AppDataMap(Language.Grammar.CaseSensitive);
            Initialize();
        }

        public ScriptApp(LanguageRuntime runtime) {
            Runtime = runtime;
            Language = Runtime.Language;
            DataMap = new AppDataMap(Language.Grammar.CaseSensitive);
            Initialize();
        }

        public ScriptApp(AppDataMap dataMap) {
            DataMap = dataMap;
            Initialize();
        }

        public LanguageData Language { get; }

        public LanguageRuntime Runtime { get; }

        public Parser Parser { get; private set; }

        public AppDataMap DataMap { get; }

        public Scope[] StaticScopes { get; private set; }

        public Scope MainScope { get; private set; }

        public IDictionary<string, object> Globals { get; private set; }

        public StringBuilder OutputBuffer { get; } = new StringBuilder();

        // Current mode/status variables
        public AppStatus Status { get; private set; }

        public long EvaluationTime { get; private set; }

        public Exception LastException { get; private set; }

        public bool RethrowExceptions { get; set; } = true;

        //the root node of the last executed script
        public ParseTree LastScript { get; private set; }

        [SecuritySafeCritical]
        private void Initialize() {
            Parser = new Parser(Language);
            //Create static scopes
            MainScope = new Scope(DataMap.MainModule.ScopeInfo, null, null, null);
            StaticScopes = new Scope[DataMap.StaticScopeInfos.Count];
            StaticScopes[0] = MainScope;
            Globals = MainScope.AsDictionary();
        }

        public LogMessageList GetParserMessages() {
            return Parser.Context.CurrentParseTree.ParserMessages;
        }

        // Utilities
        public IEnumerable<Assembly> GetImportAssemblies() {
            //simple default case - return all assemblies loaded in domain
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        public ParseMode ParserMode {
            get => Parser.Context.Mode;
            set => Parser.Context.Mode = value;
        }

        #region Evaluation
        public object Evaluate(string script) {
            try {
                var parsedScript = Parser.Parse(script);
                if (parsedScript.HasErrors) {
                    Status = AppStatus.SyntaxError;
                    if (RethrowExceptions) {
                        throw new ScriptException("Syntax errors found.");
                    }

                    return null;
                }

                if (ParserMode == ParseMode.CommandLine && Parser.Context.Status == ParserStatus.AcceptedPartial) {
                    Status = AppStatus.WaitingMoreInput;
                    return null;
                }
                LastScript = parsedScript;
                var result = EvaluateParsedScript();
                return result;
            } catch (ScriptException) {
                throw;
            } catch (Exception ex) {
                LastException = ex;
                Status = AppStatus.Crash;
                return null;
            }
        }

        // Irony interpreter requires that once a script is executed in a ScriptApp, it is bound to AppDataMap object, 
        // and all later script executions should be performed only in the context of the same app (or at least by an App with the same DataMap).
        // The reason is because the first execution sets up a data-binding fields, like slots, scopes, etc, which are bound to ScopeInfo objects, 
        // which in turn is part of DataMap.
        public object Evaluate(ParseTree parsedScript) {
            Util.Ensure(parsedScript.Root.AstNode != null, "Root AST node is null, cannot evaluate script. Create AST tree first.");
            var root = parsedScript.Root.AstNode as AstNode;
            Util.Ensure(root != null,
                "Root AST node {0} is not a subclass of Irony.Interpreter.AstNode. ScriptApp cannot evaluate this script.", parsedScript.Root.AstNode.GetType());
            Util.Ensure(root.Parent == null || root.Parent == DataMap.ProgramRoot,
                "Cannot evaluate parsed script. It had been already evaluated in a different application.");
            LastScript = parsedScript;
            return EvaluateParsedScript();
        }

        public object Evaluate() {
            Util.Ensure(LastScript != null, "No previously parsed/evaluated script.");
            return EvaluateParsedScript();
        }

        //Actual implementation
        private object EvaluateParsedScript() {
            LastScript.Tag = DataMap;
            var root = LastScript.Root.AstNode as AstNode;
            if (root == null) {
                throw new InvalidCastException();
            }
            root.DependentScopeInfo = MainScope.Info;

            Status = AppStatus.Evaluating;
            ScriptThread thread = null;
            SourceLocation scriptLocation;
            try {
                thread = new ScriptThread(this);
                var result = root.Evaluate(thread);
                if (result != null) {
                    thread.App.WriteLine(result.ToString());
                }
                Status = AppStatus.Ready;
                return result;
            } catch (ScriptException se) {
                Status = AppStatus.RuntimeError;
                scriptLocation = thread?.CurrentNode?.Location ?? new SourceLocation();
                se.Location = scriptLocation;
                se.ScriptStackTrace = thread?.GetStackTrace();
                LastException = se;
                if (RethrowExceptions) {
                    throw;
                }
                return null;
            } catch (Exception ex) {
                Status = AppStatus.RuntimeError;
                scriptLocation = thread?.CurrentNode?.Location ?? new SourceLocation();
                var se = new ScriptException(ex.Message, ex, scriptLocation, thread?.GetStackTrace());
                LastException = se;
                if (RethrowExceptions) {
                    throw se;
                }
                return null;
            }
        }
        #endregion

        #region Output writing
        #region ConsoleWrite event
        public event EventHandler<ConsoleWriteEventArgs> ConsoleWrite;

        private void OnConsoleWrite(string text) {
            ConsoleWrite?.Invoke(this, new ConsoleWriteEventArgs(text));
        }
        #endregion

        public void Write(string text) {
            lock (_lockObject) {
                OnConsoleWrite(text);
                OutputBuffer.Append(text);
            }
        }

        public void WriteLine(string text) {
            lock (_lockObject) {
                OnConsoleWrite(text + Environment.NewLine);
                OutputBuffer.AppendLine(text);
            }
        }

        public void ClearOutputBuffer() {
            lock (_lockObject) {
                OutputBuffer.Clear();
            }
        }

        public string GetOutput() {
            lock (_lockObject) {
                return OutputBuffer.ToString();
            }
        }
        #endregion

        private readonly object _lockObject = new object();
        private IList<Assembly> _importedAssemblies = new List<Assembly>();

    }

}
