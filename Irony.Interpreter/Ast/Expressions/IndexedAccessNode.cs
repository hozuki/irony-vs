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
using System.Collections;
using System.Linq;
using System.Reflection;
using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {

    public sealed class IndexedAccessNode : AstNode {

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            _target = AddChild("Target", nodes.First());
            _index = AddChild("Index", nodes.Last());
            AsString = "[" + _index + "]";
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            object result;
            var targetValue = _target.Evaluate(thread);
            if (targetValue == null) {
                thread.ThrowScriptError("Target object is null.");
            }
            var type = targetValue.GetType();
            var indexValue = _index.Evaluate(thread);
            //string and array are special cases
            if (type == typeof(string)) {
                var sTarget = (string)targetValue;
                var iIndex = Convert.ToInt32(indexValue);
                result = sTarget[iIndex];
            } else if (type.IsArray) {
                var arr = (Array)targetValue;
                var iIndex = Convert.ToInt32(indexValue);
                result = arr.GetValue(iIndex);
            } else if (targetValue is IDictionary dict) {
                result = dict[indexValue];
            } else {
                // Cannot use IndexerNameAttribute, see:
                // https://social.msdn.microsoft.com/Forums/en-US/60de101a-278d-4674-bc1a-0a04210d566c/identifying-the-indexername-attribute-on-an-indexer-property?forum=vstscode
                var defaultMemberAttr = type.GetCustomAttribute<DefaultMemberAttribute>();
                var indexerName = defaultMemberAttr?.MemberName ?? "Item";
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.InvokeMethod;
                result = type.InvokeMember("get_" + indexerName, flags, null, targetValue, new[] { indexValue });
            }
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void DoSetValue(ScriptThread thread, object value) {
            thread.CurrentNode = this;  //standard prolog
            var targetValue = _target.Evaluate(thread);
            if (targetValue == null) {
                thread.ThrowScriptError("Target object is null.");
            }

            var type = targetValue.GetType();
            var indexValue = _index.Evaluate(thread);
            //string and array are special cases
            if (type == typeof(string)) {
                thread.ThrowScriptError("String is read-only.");
            } else if (type.IsArray) {
                var arr = (Array)targetValue;
                var iIndex = Convert.ToInt32(indexValue);
                arr.SetValue(value, iIndex);
            } else if (targetValue is IDictionary dict) {
                dict[indexValue] = value;
            } else {
                // Cannot use IndexerNameAttribute, see:
                // https://social.msdn.microsoft.com/Forums/en-US/60de101a-278d-4674-bc1a-0a04210d566c/identifying-the-indexername-attribute-on-an-indexer-property?forum=vstscode
                var defaultMemberAttr = type.GetCustomAttribute<DefaultMemberAttribute>();
                var indexerName = defaultMemberAttr?.MemberName ?? "Item";
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.InvokeMethod;
                type.InvokeMember("set_" + indexerName, flags, null, targetValue, new[] { indexValue, value });
            }
            thread.CurrentNode = Parent; //standard epilog
        }

        private AstNode _target;
        private AstNode _index;

    }

}
