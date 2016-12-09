using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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

        /// <summary>
        /// Evénement de binding et scrolldown lorsque l'utilisateur appuie sur Entrée dans la barre de chat.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateTextBoxBinding(sender as TextBox);
                KeepScrollDown();
            }
        }

        /// <summary>
        /// Evénement de binding et scrolldown lors du clic sur le bouton d'envoi de message simple.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_send_Click(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxBinding(textBox_msg);
            KeepScrollDown();
        }

        /// <summary>
        /// Lors de l'envoi d'un fichier en cliquant sur le bouton d'envoi, déclenche le binding 
        /// entre le texte de la textbox de chat et l'objet CurrentMsg de classe Message dans MainViewModel.cs,
        /// avec un format qui permet le traitement de la demande côté serveur.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_sendFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                textBox_msg.Text = "@File" + dialog.FileName;
                UpdateTextBoxBinding(textBox_msg);
                KeepScrollDown();
            }
        }

        /// <summary>
        /// Déclenche le binding entre le texte de la textbox de chat et l'objet CurrentMsg de classe Message dans MainViewModel.cs
        /// </summary>
        /// <param name="tBox"></param>
        private void UpdateTextBoxBinding(TextBox tBox)
        {
            DependencyProperty prop = TextBox.TextProperty;
            BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
            if (binding != null) { binding.UpdateSource(); }
            tBox.Text = "";
        }

        /// <summary>
        /// Maintient le scroll au niveau du dernier message.
        /// </summary>
        private void KeepScrollDown()
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
