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

using System.Collections.Generic;

namespace Irony.Ast {

    // Grammar Explorer uses this interface to discover and display the AST tree after parsing the input
    // (Grammar Explorer additionally uses ToString method of the node to get the text representation of the node)
    public interface IBrowsableAstNode {

        int Position { get; }
        IEnumerable<IBrowsableAstNode> GetChildNodes();

    }

}
