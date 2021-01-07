//21-01-07 RMA TFS-XXX Creation of File.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            //Handle button press in 'EDIT' mode.
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
            if (isNumeric && button != "0")
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
            tbScreen.Text += "=";
        }
    }
}
