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
    public static class ValidateTokenEventArgsExtensions {

        public static void ReplaceToken(this ValidateTokenEventArgs e, Token token) {
            e.Context.CurrentToken = token;
        }

        public static void SetError(this ValidateTokenEventArgs e, string errorMessage, params object[] messageArgs) {
            e.Context.CurrentToken = e.Context.CreateErrorToken(errorMessage, messageArgs);
        }

        //Rejects the token; use it when there's more than one terminal that can be used to scan the input and ValidateToken is used
        // to help Scanner make the decision. Once the token is rejected, the scanner will move to the next Terminal (with lower priority)
        // and will try to produce token. 
        public static void RejectToken(this ValidateTokenEventArgs e) {
            e.Context.CurrentToken = null;
        }

    }
}
