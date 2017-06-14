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
using Irony.Interpreter.Ast;

namespace Irony.Interpreter {

    public sealed class ClrMethodBindingTargetInfo : ClrInteropBindingTargetInfo, ICallTarget { //The object works as ICallTarget itself

        public ClrMethodBindingTargetInfo(Type declaringType, string methodName, object instance = null) 
            : base(methodName, ClrTargetType.Method) {
            DeclaringType = declaringType;
            Instance = instance;
            _invokeFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic;
            if (Instance == null) {
                _invokeFlags |= BindingFlags.Static;
            } else {
                _invokeFlags |= BindingFlags.Instance;
            }
            _binding = new ConstantBinding(this, this);
            //The object works as CallTarget itself; the "as" conversion is not needed in fact, we do it just to underline the role
        }

        public object Instance { get; }

        public Type DeclaringType { get; }

        public override Binding Bind(BindingRequest request) {
            return _binding;
        }

        #region ICalllable.Call implementation
        public object Call(ScriptThread thread, object[] args) {
            // TODO: fix this. Currently doing it slow but easy way, through reflection
            if (args != null && args.Length == 0) {
                args = null;
            }
            var result = DeclaringType.InvokeMember(Symbol, _invokeFlags, null, Instance, args);
            return result;
        }
        #endregion

        private readonly BindingFlags _invokeFlags;
        private readonly Binding _binding;

    }

}
