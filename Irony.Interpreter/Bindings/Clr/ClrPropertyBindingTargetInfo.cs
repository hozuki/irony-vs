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

    public sealed class ClrPropertyBindingTargetInfo : ClrInteropBindingTargetInfo {

        public ClrPropertyBindingTargetInfo(PropertyInfo property, object instance)
            : base(property.Name, ClrTargetType.Property) {
            Property = property;
            Instance = instance;
            _binding = new Binding(this) {
                GetValueRef = GetPropertyValue,
                SetValueRef = SetPropertyValue
            };
        }

        public object Instance { get; }

        public PropertyInfo Property { get; }

        public override Binding Bind(BindingRequest request) {
            return _binding;
        }

        private object GetPropertyValue(ScriptThread thread) {
            var result = Property.GetValue(Instance, null);
            return result;
        }

        private void SetPropertyValue(ScriptThread thread, object value) {
            Property.SetValue(Instance, value, null);
        }

        private readonly Binding _binding;

    }

}