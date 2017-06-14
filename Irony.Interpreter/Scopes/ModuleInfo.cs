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

    public sealed class ModuleInfo : IBindingSource {

        public ModuleInfo(string name, string fileName, ScopeInfo scopeInfo) {
            Name = name;
            FileName = fileName;
            ScopeInfo = scopeInfo;
        }

        public string Name { get; }

        public string FileName { get; }

        //scope for module variables
        public ScopeInfo ScopeInfo { get; }

        public BindingSourceList Imports { get; } = new BindingSourceList();

        //Used for imported modules
        public Binding BindToExport(BindingRequest request) {
            return null;
        }

        Binding IBindingSource.Bind(BindingRequest request) {
            return BindToExport(request);
        }

    }

}
