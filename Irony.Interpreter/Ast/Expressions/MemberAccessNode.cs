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

using System.Reflection;
using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {

    //For now we do not support dotted namespace/type references like System.Collections or System.Collections.List.
    // Only references to objects like 'objFoo.Name' or 'objFoo.DoStuff()'
    public sealed class MemberAccessNode : AstNode {

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            _left = AddChild("Target", nodes[0]);
            var right = nodes[nodes.Count - 1];
            _memberName = right.FindTokenAndGetText();
            ErrorAnchor = right.Span.Location;
            AsString = "." + _memberName;
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            object result;
            var leftValue = _left.Evaluate(thread);
            if (leftValue == null) {
                thread.ThrowScriptError("Target object is null.");
            }

            var type = leftValue.GetType();
            var members = type.GetMember(_memberName);
            if (members.Length == 0) {
                thread.ThrowScriptError("Member {0} not found in object of type {1}.", _memberName, type);
            }

            var member = members[0];
            switch (member.MemberType) {
                case MemberTypes.Property:
                    var propInfo = (PropertyInfo)member;
                    result = propInfo.GetValue(leftValue, null);
                    break;
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)member;
                    result = fieldInfo.GetValue(leftValue);
                    break;
                case MemberTypes.Method:
                    result = new ClrMethodBindingTargetInfo(type, _memberName, leftValue); //this bindingInfo works as a call target
                    break;
                default:
                    thread.ThrowScriptError("Invalid member type ({0}) for member {1} of type {2}.", member.MemberType, _memberName, type);
                    result = null;
                    break;
            }
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void DoSetValue(ScriptThread thread, object value) {
            thread.CurrentNode = this;  //standard prolog
            var leftValue = _left.Evaluate(thread);
            if (leftValue == null) {
                thread.ThrowScriptError("Target object is null.");
            }

            var type = leftValue.GetType();
            var members = type.GetMember(_memberName);
            if (members.Length == 0) {
                thread.ThrowScriptError("Member {0} not found in object of type {1}.", _memberName, type);
            }

            var member = members[0];
            switch (member.MemberType) {
                case MemberTypes.Property:
                    var propInfo = (PropertyInfo)member;
                    propInfo.SetValue(leftValue, value, null);
                    break;
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)member;
                    fieldInfo.SetValue(leftValue, value);
                    break;
                default:
                    thread.ThrowScriptError("Cannot assign to member {0} of type {1}.", _memberName, type);
                    break;
            }
            thread.CurrentNode = Parent; //standard epilog
        }

        private AstNode _left;
        private string _memberName;

    }

}
