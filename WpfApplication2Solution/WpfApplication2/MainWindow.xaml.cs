using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication2.ViewModel;

namespace WpfApplication2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {           
            if (e.Key == Key.Enter)
            {
                //Console.WriteLine("enter");
                UpdateTextBoxBinding(sender as TextBox);
                KeepScrollDown();
            }           
        }

        private void button_send_Click(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("click");
            UpdateTextBoxBinding(textBox_msg);
            KeepScrollDown();
            /*var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            CommonFileDialogResult result = dialog.ShowDialog();*/
            //if (result)) { Console.WriteLine(dialog.FileName); }
        }


        private void UpdateTextBoxBinding(TextBox tBox)
        {
            DependencyProperty prop = TextBox.TextProperty;
            BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
            if (binding != null) { binding.UpdateSource(); }
            tBox.Text = "";

        }

        private void KeepScrollDown ()
        {
            if (VisualTreeHelper.GetChildrenCount(listBox_chat) > 0)
            {
                Border border = (Border)VisualTreeHelper.GetChild(listBox_chat, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

    }
}
