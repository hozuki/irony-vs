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

using System.Linq.Expressions;

namespace Irony.Interpreter.Ast {

    public sealed class OperatorHandler {

        public OperatorHandler(bool languageCaseSensitive) {
            _registeredOperators = new OperatorInfoDictionary(languageCaseSensitive);
            BuildDefaultOperatorMappings();
        }

        public ExpressionType GetOperatorExpressionType(string symbol) {
            if (_registeredOperators.TryGetValue(symbol, out OperatorInfo opInfo)) {
                return opInfo.ExpressionType;
            } else {
                return CustomExpressionTypes.NotAnExpression;
            }
        }

        public static ExpressionType GetUnaryOperatorExpressionType(string symbol) {
            switch (symbol.ToLowerInvariant()) {
                case "+": return ExpressionType.UnaryPlus;
                case "-": return ExpressionType.Negate;
                case "!":
                case "not":
                case "~":
                    return ExpressionType.Not;
                default:
                    return CustomExpressionTypes.NotAnExpression;
            }
        }


        public static ExpressionType GetBinaryOperatorForAugmented(ExpressionType augmented) {
            switch (augmented) {
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                    return ExpressionType.AddChecked;
                case ExpressionType.AndAssign:
                    return ExpressionType.And;
                case ExpressionType.Decrement:
                    return ExpressionType.SubtractChecked;
                case ExpressionType.DivideAssign:
                    return ExpressionType.Divide;
                case ExpressionType.ExclusiveOrAssign:
                    return ExpressionType.ExclusiveOr;
                case ExpressionType.LeftShiftAssign:
                    return ExpressionType.LeftShift;
                case ExpressionType.ModuloAssign:
                    return ExpressionType.Modulo;
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                    return ExpressionType.MultiplyChecked;
                case ExpressionType.OrAssign:
                    return ExpressionType.Or;
                case ExpressionType.RightShiftAssign:
                    return ExpressionType.RightShift;
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                    return ExpressionType.SubtractChecked;
                default:
                    return CustomExpressionTypes.NotAnExpression;
            }
        }

        private OperatorInfoDictionary BuildDefaultOperatorMappings() {
            var dict = _registeredOperators;
            dict.Clear();
            var p = 0; //precedence

            p += 10;
            dict.Add("=", ExpressionType.Assign, p);
            dict.Add("+=", ExpressionType.AddAssignChecked, p);
            dict.Add("-=", ExpressionType.SubtractAssignChecked, p);
            dict.Add("*=", ExpressionType.MultiplyAssignChecked, p);
            dict.Add("/=", ExpressionType.DivideAssign, p);
            dict.Add("%=", ExpressionType.ModuloAssign, p);
            dict.Add("|=", ExpressionType.OrAssign, p);
            dict.Add("&=", ExpressionType.AndAssign, p);
            dict.Add("^=", ExpressionType.ExclusiveOrAssign, p);

            p += 10;
            dict.Add("==", ExpressionType.Equal, p);
            dict.Add("!=", ExpressionType.NotEqual, p);
            dict.Add("<>", ExpressionType.NotEqual, p);

            p += 10;
            dict.Add("<", ExpressionType.LessThan, p);
            dict.Add("<=", ExpressionType.LessThanOrEqual, p);
            dict.Add(">", ExpressionType.GreaterThan, p);
            dict.Add(">=", ExpressionType.GreaterThanOrEqual, p);

            p += 10;
            dict.Add("|", ExpressionType.Or, p);
            dict.Add("or", ExpressionType.Or, p);
            dict.Add("||", ExpressionType.OrElse, p);
            dict.Add("orelse", ExpressionType.OrElse, p);
            dict.Add("^", ExpressionType.ExclusiveOr, p);
            dict.Add("xor", ExpressionType.ExclusiveOr, p);

            p += 10;
            dict.Add("&", ExpressionType.And, p);
            dict.Add("and", ExpressionType.And, p);
            dict.Add("&&", ExpressionType.AndAlso, p);
            dict.Add("andalso", ExpressionType.AndAlso, p);

            p += 10;
            dict.Add("!", ExpressionType.Not, p);
            dict.Add("not", ExpressionType.Not, p);

            p += 10;
            dict.Add("<<", ExpressionType.LeftShift, p);
            dict.Add(">>", ExpressionType.RightShift, p);

            p += 10;
            dict.Add("+", ExpressionType.AddChecked, p);
            dict.Add("-", ExpressionType.SubtractChecked, p);

            p += 10;
            dict.Add("*", ExpressionType.MultiplyChecked, p);
            dict.Add("/", ExpressionType.Divide, p);
            dict.Add("%", ExpressionType.Modulo, p);
            dict.Add("**", ExpressionType.Power, p);

            p += 10;
            dict.Add("??", ExpressionType.Coalesce, p);
            dict.Add("?", ExpressionType.Conditional, p);

            return dict;
        }

        private readonly OperatorInfoDictionary _registeredOperators;

    }

}
