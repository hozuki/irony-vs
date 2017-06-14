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

namespace Irony.Interpreter {

    public sealed class ClrFieldBindingTargetInfo : ClrInteropBindingTargetInfo {

        public ClrFieldBindingTargetInfo(FieldInfo field, object instance)
            : base(field.Name, ClrTargetType.Field) {
            Field = field;
            Instance = instance;
            _binding = new Binding(this);
            _binding.GetValueRef = GetPropertyValue;
            _binding.SetValueRef = SetPropertyValue;
        }

        public object Instance { get; }

        public FieldInfo Field { get; }

        public override Binding Bind(BindingRequest request) {
            return _binding;
        }

        private object GetPropertyValue(ScriptThread thread) {
            var result = Field.GetValue(Instance);
            return result;
        }

        private void SetPropertyValue(ScriptThread thread, object value) {
            Field.SetValue(Instance, value);
        }

        private readonly Binding _binding;

    }

}