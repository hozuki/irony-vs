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

namespace Irony.Interpreter {

    // The class contains information about built-in function. It has double purpose. 
    // First, it is used as a BindingTargetInfo instance (meta-data) for a binding to a built-in function. 
    // Second, we use it as a reference to a custom built-in method that we store in LanguageRuntime.BuiltIns table. 
    // For this, we make it implement IBindingSource - we can add it to BuiltIns table of LanguageRuntime, which is a table of IBindingSource instances.
    // Being IBindingSource, it can produce a binding object to the target method - singleton in fact; 
    // the same binding object is used for all calls to the method from all function-call AST nodes. 
    public sealed class BuiltInCallableTargetInfo : BindingTargetInfo, IBindingSource {

        public BuiltInCallableTargetInfo(BuiltInMethod method, string methodName, int minParamCount = 0, int maxParamCount = 0, string parameterNames = null)
            : this(new BuiltInCallTarget(method, methodName, minParamCount, maxParamCount, parameterNames)) {
        }

        public BuiltInCallableTargetInfo(BuiltInCallTarget target)
            : base(target.Name, BindingTargetType.BuiltInObject) {
            BindingInstance = new ConstantBinding(target, this);
        }

        //A singleton binding instance; we share it for all AST nodes (function call nodes) that call the method. 
        public Binding BindingInstance { get; }

        //Implement IBindingSource.Bind
        public Binding Bind(BindingRequest request) {
            return BindingInstance;
        }

    }

}
