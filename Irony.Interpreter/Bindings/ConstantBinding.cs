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

    //Binding to a "fixed", constant value
    public class ConstantBinding : Binding {

        public ConstantBinding(object target, BindingTargetInfo targetInfo)
            : base(targetInfo) {
            Target = target;
            GetValueRef = GetValue;
            IsConstant = true;
        }

        public object Target { get; }

        public object GetValue(ScriptThread thread) {
            return Target;
        }

    }

}
