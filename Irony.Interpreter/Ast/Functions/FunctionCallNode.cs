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

using Irony.Ast;
using Irony.Interpreter.Utilities;
using Irony.Parsing;
using Irony.Utilities;

namespace Irony.Interpreter.Ast {

    //A node representing function call. Also handles Special Forms
    public sealed class FunctionCallNode : AstNode {

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            _targetRef = AddChild("Target", nodes[0]);
            _targetRef.UseType = NodeUseType.CallTarget;
            _targetName = nodes[0].FindTokenAndGetText();
            _arguments = AddChild("Args", nodes[1]);
            AsString = "Call " + _targetName;
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            SetupEvaluateMethod(thread);
            var result = Evaluate(thread);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        private void SetupEvaluateMethod(ScriptThread thread) {
            var languageTailRecursive = thread.Runtime.Language.Grammar.LanguageFlags.IsSet(LanguageFlags.TailRecursive);
            lock (LockObject) {
                var target = _targetRef.Evaluate(thread);
                if (target is SpecialForm) {
                    _specialForm = target as SpecialForm;
                    _specialFormArgs = _arguments.ChildNodes.ToArray();
                    Evaluate = EvaluateSpecialForm;
                } else {
                    if (languageTailRecursive) {
                        var isTail = Flags.IsSet(AstNodeFlags.IsTail);
                        if (isTail) {
                            Evaluate = EvaluateTail;
                        } else {
                            Evaluate = EvaluateWithTailCheck;
                        }
                    } else {
                        Evaluate = EvaluateNoTail;
                    }
                }
            }
        }

        // Evaluation for special forms
        private object EvaluateSpecialForm(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var result = _specialForm(thread, _specialFormArgs);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        // Evaluation for non-tail languages
        private object EvaluateNoTail(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var target = _targetRef.Evaluate(thread);
            var iCall = target as ICallTarget;
            if (iCall == null) {
                thread.ThrowScriptError(Resources.ErrVarIsNotCallable, _targetName);
            }

            var args = (object[])_arguments.Evaluate(thread);
            object result = iCall.Call(thread, args);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        //Evaluation for tailed languages
        private object EvaluateTail(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var target = _targetRef.Evaluate(thread);
            var iCall = target as ICallTarget;
            if (iCall == null) {
                thread.ThrowScriptError(Resources.ErrVarIsNotCallable, _targetName);
            }

            var args = (object[])_arguments.Evaluate(thread);
            thread.Tail = iCall;
            thread.TailArgs = args;
            thread.CurrentNode = Parent; //standard epilog
            return null;
        }

        private object EvaluateWithTailCheck(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var target = _targetRef.Evaluate(thread);
            var iCall = target as ICallTarget;
            if (iCall == null) {
                thread.ThrowScriptError(Resources.ErrVarIsNotCallable, _targetName);
            }

            var args = (object[])_arguments.Evaluate(thread);
            var result = iCall.Call(thread, args);
            //Note that after invoking tail we can get another tail. 
            // So we need to keep calling tails while they are there.
            while (thread.Tail != null) {
                var tail = thread.Tail;
                var tailArgs = thread.TailArgs;
                thread.Tail = null;
                thread.TailArgs = null;
                result = tail.Call(thread, tailArgs);
            }
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        private AstNode _targetRef;
        private AstNode _arguments;
        private string _targetName;
        private SpecialForm _specialForm;
        private AstNode[] _specialFormArgs;

    }

}
