//21-01-07 RMA TFS-XXX Creation of File.

using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //List of all modified operations is saved here.
        private static Dictionary<string, string> mods = new Dictionary<string, string>();

        //Temporary data of currently modified operation.
        private static string modButton = "";
        private static string modText = "";

        //Messages
        private static readonly string msg1 = "Please select a button to edit from A to V.";
        private static readonly string msg2 = "Editing, click 'Save' to continue. use 'x' and 'y'. (e.g. x+y)";


        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle default button clicks.
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string button = (sender as Button).Content.ToString();
            var isNumeric = int.TryParse(button, out int n);
            var isLetter = (button.Length == 1 && button.Any(x => char.IsLetter(x)));

            //Handle button press in 'EDIT' mode- Instead of writting on screen it goes into configuration instead.
            if (btEdit.Content.ToString() == "CANCEL" && isLetter)
            {
                modButton = button;
                tbScreen.Text = msg2;
                btEdit.Content = "SAVE";
                rtbCode.Document.Blocks.Clear();
                if (mods.ContainsKey(modButton))
                {
                    rtbCode.Document.Blocks.Add(new Paragraph(new Run(mods[modButton])));
                }
            }
            //Write digit to screen.
            else if (isNumeric && button != "0")
            {
                tbScreen.Text += button;
            }
            //Write function symbol to screen.
            else
            {
                if (tbScreen.Text.Length > 0 && char.IsDigit(tbScreen.Text[tbScreen.Text.Length - 1]))
                {
                    tbScreen.Text += button;
                }
            }
        }

        /// <summary>
        /// Clear the screen.
        /// </summary>
        private void Button_Click_CLR(object sender, RoutedEventArgs e)
        {
            tbScreen.Text = "";
        }

        /// <summary>
        /// Delete last symbol on the screen.
        /// </summary>
        private void Button_Click_DEL(object sender, RoutedEventArgs e)
        {
            if (tbScreen.Text.Length > 0)
                tbScreen.Text = tbScreen.Text.Remove(tbScreen.Text.Length - 1);
        }

        /// <summary>
        /// Edit button logic.
        /// </summary>
        private void Button_Click_EDIT(object sender, RoutedEventArgs e)
        {
            string button = (sender as Button).Content.ToString();

            //Clicking the 'EDIT' button allows the user to choose what button to configure.
            if (button == "EDIT")
            {
                if (tbScreen.Text != msg1 || tbScreen.Text != msg2)
                {
                    tbScreen.Text = msg1;
                    btEdit.Content = "CANCEL";
                }
            }

            //User has an option to click this button again to cancel editing.
            else if (button == "CANCEL")
            {
                tbScreen.Text = "";
                btEdit.Content = "EDIT";
                modButton = "";
                modText = "";
            }

            //After writing a moded formula, user can save it by clicking this button once again.
            else if (button == "SAVE")
            {
                tbScreen.Text = "";
                btEdit.Content = "EDIT";
                modText = new TextRange(rtbCode.Document.ContentStart, rtbCode.Document.ContentEnd).Text;

                //Add modified button to dictionary or modify it if its already in dictionary.
                if (!mods.ContainsKey(modButton))
                {
                    mods.Add(modButton, modText);
                }
                else
                {
                    mods[modButton] = modText;
                }

                rtbCode.Document.Blocks.Clear();
            }
        }

        /// <summary>
        /// Calculate the solution.
        /// </summary>
        private void Button_Click_EQUAL(object sender, RoutedEventArgs e)
        {
            //Hotfix for cases that don't end in numbers.
            var endsInNum = int.TryParse(tbScreen.Text.Last().ToString(), out int n);
            if (!endsInNum)
            {
                tbScreen.Text += 0;
            }
            tbScreen.Text += "=";

            //Setup runtime compiler
            CSharpCodeProvider cscp = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });
            CompilerParameters parameters = new CompilerParameters(new[] { "mscorlib.dll", "System.Core.dll" }, "Result.exe", true)
            {
                GenerateExecutable = true
            };
            string source = GetSource();

            CompilerResults results = cscp.CompileAssemblyFromSource(parameters, source);
            //Debugger
            if (results.Errors.HasErrors)
            {
                results.Errors.Cast<CompilerError>().ToList().ForEach(error => source += error.ErrorText + "\r\n");
            }
            //Execute solution
            else
            {
                tbScreen.Text = "";
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "/" + "Result.exe");
            }
        }

        /// <summary>
        /// Source code for the runtime compiler program.
        /// </summary>
        /// <returns>C# program</returns>
        public string GetSource()
        {
            //Save autogenerated source code in these strings.
            string operationCalls = "";
            string operationWhile = "1==0";
            string operationMethods = "";

            //Generate source code from code in mods dictionarry.
            foreach (var operation in mods)
            {
                //Add calls to the new methods.
                operationCalls += "\n" +
                    "\nif (operation ==\"" + operation.Key + "\" )" +
                    "\n{" +
                    "\nnewOperations.Add(Mod" + operation.Key + "(Convert.ToInt64(operations[cnt - 1]), Convert.ToInt64(operations[cnt + 1])));" +
                    "\n}\n";

                //Close do-while loop when equations are solved
                operationWhile += " || operations.Contains(\"" + operation.Key + "\")";

                //Add the actual methods.
                operationMethods += "\nprivate static string Mod" + operation.Key + "(Int64 x = 1, Int64 y = 1)" +
                    "\n{" +
                    "\nreturn (" + operation.Value +
                    ").ToString();" +
                    "\n}";
            }
            #region Some voodoo magic going on here.
            string result = "\n" +
    "\nusing System;" +
    "\nusing System.Collections.Generic;" +
    "\nusing System.Threading.Tasks;" +
    "\n" +
    "\nnamespace Calculator" +
    "\n{" +
        "\nstatic class Calculator" +
        "\n{" +
            "\nstatic void Main()" +
            "\n{" +
                //Read the formula from calculators screen
                "\nvar operatonString = \"" + tbScreen.Text + "\";" +
                "\n" +
                "\nvar cnt = 0;" +
                //Split the formula into list of numbers and operators
                "\nvar operations = StringSplit(operatonString);" +
                "\n" +
                //For each mod operator in equation, solve it by calling its method and passing X and Y variables to it.
                "\ndo" +
                "\n{" +
                    "\ncnt = 0;" +
                    "\nvar newOperations = new List<string>();" +
                    "\nforeach (var operation in operations)" +
                    "\n{" +
                        operationCalls +
                        "\ncnt++;" +
                    "\n}" +
                    "\nif (newOperations.Count > 0)" +
                    "\n{" +
                        "\noperations = newOperations;" +
                    "\n}" +
                "\n}" +
                "\nwhile (" + operationWhile + ") ;" +
                "\n" +
                //For every * and / operator in the equation, do the math.
                "\ndo" +
                "\n{" +
                    "\ncnt = 0;" +
                    "\nvar newOperations = new List<string>();" +
                    "\nforeach (var operation in operations)" +
                    "\n{" +
                        "\nif (operation ==\"*\" )" +
                        "\n{" +
                            "\nnewOperations.Add((Convert.ToInt64(operations[cnt - 1]) * Convert.ToInt64(operations[cnt + 1])).ToString());" +
                        "\n}" +
                        "\nelse if (operation == \"/\")" +
                        "\n{" +
                            "\nnewOperations.Add((Convert.ToInt64(operations[cnt - 1]) / Convert.ToInt64(operations[cnt + 1])).ToString());" +
                        "\n}" +
                        "\ncnt++;" +
                    "\n}" +
                    "\nif (newOperations.Count > 0)" +
                    "\n{" +
                    "\noperations = newOperations;" +
                    "\n}" +
                "\n} " +
                "\nwhile (operations.Contains(\"*\") || operations.Contains(\"/\")) ;" +
                "\n" +
                //For every + and - operator in the equation, do the math.
                //NOTE: Some spaghetti got spilled here. 
                //      I wanted the program to calculate '*/' before '+-', 
                //      forgot about '()' and time is runing out. PANIC!
                "\ndo" +
                "\n{" +
                    "\ncnt = 0;" +
                    "\nvar newOperations = new List<string>();" +
                    "\nforeach (var operation in operations)" +
                    "\n{" +
                        "\nif (operation ==\"+\" )" +
                        "\n{" +
                            "\nnewOperations.Add((Convert.ToInt64(operations[cnt - 1]) + Convert.ToInt64(operations[cnt + 1])).ToString());" +
                        "\n}" +
                        "\nelse if (operation == \"-\")" +
                        "\n{" +
                            "\nnewOperations.Add((Convert.ToInt64(operations[cnt - 1]) - Convert.ToInt64(operations[cnt + 1])).ToString());" +
                        "\n}" +
                    "\ncnt++;" +
                    "\n}" +
                "\nif (newOperations.Count > 0)" +
                "\n{" +
                    "\noperations = newOperations;" +
                "\n}" +
            "\n} " +
            "\nwhile (operations.Contains(\"+\") || operations.Contains(\"-\")) ;" +
            //Show the result.
            "\nConsole.WriteLine(operatonString+operations[0].ToString());" +
            "\nConsole.WriteLine(\"Press 'Enter' or close this window to continue.\");" +
            "\nConsole.ReadLine();" +
        "\n}" +
        "\n" +
            operationMethods +
        "\n" +
        //This method splits the equation into a list of numbers and operators.
        "\nprivate static List<string> StringSplit(string str)" +
        "\n{" +
            "\nList<string> functionList = new List<string>();" +
            "\nstring numb = \"\";" +
            "\n" +
                "\nforeach (var symbol in str)" +
                "\n{" +
                    "\nif (char.IsDigit(symbol))" +
                    "\n{" +
                        "\nnumb += symbol;" +
                    "\n}" +
                    "\nelse" +
                    "\n{" +
                        "\nfunctionList.Add(numb);" +
                        "\nfunctionList.Add(symbol.ToString());" +
                        "\nnumb = \"\";" +
                    "\n}" +
                "\n}" +
                "\n" +
                "\nreturn functionList;" +
            "\n}" +
            "\n" +
        "\n}" +
    "\n}";
            #endregion
            return result;
        }
    }
}