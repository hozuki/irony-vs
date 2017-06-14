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

namespace Irony.Parsing {

    //DataLiteralBase is a base class for a set of specialized terminals with a primary purpose of building data readers
    // DsvLiteral is used for reading delimiter-separated values (DSV), comma-separated format is a specific case of DSV
    // FixedLengthLiteral may be used to read values of fixed length
    public abstract class DataLiteralBase : Terminal {

        public DataLiteralBase(string name, TypeCode dataType)
            : base(name) {
            DataType = dataType;
        }

        public TypeCode DataType { get; }

        //For date format strings see MSDN help for "Custom format strings", available through help for DateTime.ParseExact(...) method
        public string DateTimeFormat { get; set; } = "d"; //standard format, identifies MM/dd/yyyy for invariant culture.

        public int IntRadix { get; set; } = 10; //Radix (base) for numeric numbers

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            try {
                var textValue = ReadBody(context, source);
                if (textValue == null) {
                    return null;
                }

                var value = ConvertValue(context, textValue);
                return source.CreateToken(this.OutputTerminal, value);
            } catch (Exception ex) {
                //we throw exception in DsvLiteral when we cannot find a closing quote for quoted value
                return context.CreateErrorToken(ex.Message);
            }
        }


        protected abstract string ReadBody(ParsingContext context, ISourceStream source);

        protected virtual object ConvertValue(ParsingContext context, string textValue) {
            switch (DataType) {
                case TypeCode.String:
                    return textValue;
                case TypeCode.DateTime:
                    return DateTime.ParseExact(textValue, DateTimeFormat, context.Culture);
                case TypeCode.Single:
                case TypeCode.Double:
                    var dValue = Convert.ToDouble(textValue, context.Culture);
                    if (DataType == TypeCode.Double) {
                        return dValue;
                    }
                    return Convert.ChangeType(dValue, DataType, context.Culture);

                default: //integer types
                    var iValue = (IntRadix == 10) ? Convert.ToInt64(textValue, context.Culture) : Convert.ToInt64(textValue, IntRadix);
                    if (DataType == TypeCode.Int64) {
                        return iValue;
                    }
                    return Convert.ChangeType(iValue, DataType, context.Culture);
            }
        }

    }

}
