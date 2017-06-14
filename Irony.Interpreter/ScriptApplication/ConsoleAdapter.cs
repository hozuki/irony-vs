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

    // Default implementation of IConsoleAdaptor with System Console as input/output. 
    public sealed class ConsoleAdapter : IConsoleAdapter {

        public ConsoleAdapter() {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        ~ConsoleAdapter() {
            Console.CancelKeyPress -= Console_CancelKeyPress;
        }

        public bool Canceled { get; set; }

        public void Write(string text) {
            Console.Write(text);
        }

        public void WriteLine(string text) {
            Console.WriteLine(text);
        }

        public void SetTextStyle(ConsoleTextStyle style) {
            switch (style) {
                case ConsoleTextStyle.Normal:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case ConsoleTextStyle.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
        }

        public int Read() {
            return Console.Read();
        }

        public string ReadLine() {
            var input = Console.ReadLine();
            Canceled = (input == null);  // Windows console method ReadLine returns null if Ctrl-C was pressed.
            return input;
        }

        public void SetTitle(string title) {
            Console.Title = title;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            e.Cancel = true; //do not kill the app yet
            Canceled = true;
        }

    }

}
