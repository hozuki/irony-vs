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

namespace Irony.Interpreter {

    // Method for adding methods to BuiltIns table in Runtime
    public static class BindingSourceTableExtensions {

        public static BindingTargetInfo AddMethod(this BindingSourceTable targets, BuiltInMethod method, string methodName,
            int minParamCount = 0, int maxParamCount = 0, string parameterNames = null) {
            var callTarget = new BuiltInCallTarget(method, methodName, minParamCount, maxParamCount, parameterNames);
            var targetInfo = new BuiltInCallableTargetInfo(callTarget);
            targets.Add(methodName, targetInfo);
            return targetInfo;
        }

        //Method for adding methods to BuiltIns table in Runtime
        public static BindingTargetInfo AddSpecialForm(this BindingSourceTable targets, SpecialForm form, string formName,
            int minChildCount = 0, int maxChildCount = 0, string parameterNames = null) {
            var formInfo = new SpecialFormBindingInfo(formName, form, minChildCount, maxChildCount, parameterNames);
            targets.Add(formName, formInfo);
            return formInfo;
        }

        public static void ImportStaticMembers(this BindingSourceTable targets, Type fromType) {
            var members = fromType.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (var member in members) {
                if (targets.ContainsKey(member.Name)) {
                    //do not import overloaded methods several times
                    continue;
                }
                switch (member.MemberType) {
                    case MemberTypes.Method:
                        targets.Add(member.Name, new ClrMethodBindingTargetInfo(fromType, member.Name));
                        break;
                    case MemberTypes.Property:
                        targets.Add(member.Name, new ClrPropertyBindingTargetInfo(member as PropertyInfo, null));
                        break;
                    case MemberTypes.Field:
                        targets.Add(member.Name, new ClrFieldBindingTargetInfo(member as FieldInfo, null));
                        break;
                }
            }
        }

    }

}
