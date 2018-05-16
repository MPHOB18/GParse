﻿using System;
using System.Linq;
using GParse.Common;

namespace GParse.CLI.AST
{
    public class FunctionCallExpression : ASTNode
    {
        public readonly String Identifier;
        public readonly ASTNode[] Arguments;

        public FunctionCallExpression ( String ident, ASTNode[] args )
        {
            this.Identifier = ident;
            this.Arguments = args;
        }

        public override String ToString ( ) => $"FuncCall<{this.Identifier}, {String.Join ( ", ", this.Arguments.Select ( x => x.ToString ( ) ) )}>";
    }
}
