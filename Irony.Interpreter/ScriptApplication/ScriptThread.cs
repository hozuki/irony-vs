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

    /// <summary>
    ///  Represents a running thread in script application.  
    /// </summary>
    public sealed class ScriptThread : IBindingSource {

        public ScriptThread(ScriptApp app) {
            App = app;
            Runtime = App.Runtime;
            CurrentScope = app.MainScope;
        }

        public ScriptApp App { get; }
        public LanguageRuntime Runtime { get; }

        public Scope CurrentScope { get; private set; }

        public AstNode CurrentNode { get; internal set; }

        // Tail call parameters
        public ICallTarget Tail { get; internal set; }

        public object[] TailArgs { get; internal set; }

        public void PushScope(ScopeInfo scopeInfo, object[] parameters) {
            CurrentScope = new Scope(scopeInfo, CurrentScope, CurrentScope, parameters);
        }

        public void PushClosureScope(ScopeInfo scopeInfo, Scope closureParent, object[] parameters) {
            CurrentScope = new Scope(scopeInfo, CurrentScope, closureParent, parameters);
        }

        public void PopScope() {
            CurrentScope = CurrentScope.Caller;
        }

        public Binding Bind(string symbol, BindingRequestFlags options) {
            var request = new BindingRequest(this, CurrentNode, symbol, options);
            var binding = Bind(request);
            if (binding == null) {
                ThrowScriptError("Unknown symbol '{0}'.", symbol);
            }
            return binding;
        }

        #region Exception handling
        [ContractAnnotation("=> halt")]
        public object HandleError(Exception exception) {
            if (exception is ScriptException) {
                throw exception;
            }
            var stack = GetStackTrace();
            var rex = new ScriptException(exception.Message, exception, CurrentNode.ErrorAnchor, stack);
            throw rex;
        }

        // Throws ScriptException exception.
        [StringFormatMethod("message")]
        [ContractAnnotation("=> halt")]
        public void ThrowScriptError(string message, params object[] args) {
            if (args != null && args.Length > 0) {
                message = string.Format(message, args);
            }
            var loc = GetCurrentLocation();
            var stack = GetStackTrace();
            throw new ScriptException(message, null, loc, stack);
        }

        //TODO: add construction of Script Call stack
        public ScriptStackTrace GetStackTrace() {
            return new ScriptStackTrace();
        }

        private SourceLocation GetCurrentLocation() {
            return CurrentNode?.Location ?? new SourceLocation();
        }
        #endregion
        
        #region IBindingSource Members
        public Binding Bind(BindingRequest request) {
            return Runtime.Bind(request);
        }
        #endregion

    }

}
