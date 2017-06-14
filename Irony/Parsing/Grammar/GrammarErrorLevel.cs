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

namespace Irony.Parsing {

    public enum GrammarErrorLevel {
        NoError, //used only for max error level when there are no errors
        Info,
        Warning,
        Conflict, //shift-reduce or reduce-reduce conflict
        Error,    //severe grammar error, parser construction cannot continue
        InternalError,  //internal Irony error
    }

}
