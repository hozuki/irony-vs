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

namespace Irony.Interpreter {

    public class ClrTypeBindingTargetInfo : ClrInteropBindingTargetInfo {

        public ClrTypeBindingTargetInfo(Type type)
            : base(type.Name, ClrTargetType.Type) {
            _binding = new ConstantBinding(type, this);
        }

        public override Binding Bind(BindingRequest request) {
            return _binding;
        }

        private readonly ConstantBinding _binding;

    }

}
