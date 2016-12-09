using System.Windows;

namespace WpfApplication2
{
    /// <summary>
    /// Interaction logique pour Dialog.xaml
    /// </summary>
    partial class MyDialog : Window
    {
        public MyDialog()
        {
            InitializeComponent();
        }

        //Booléen qui vaut true si "Sign in" et false si "sign up".
        public bool SignIn { get; private set; }

        //Booléen qui vaut true quand l'utilisateur ferme la fenêtre.
        public bool Exit { get; private set; }

        //Les 4 strings correspondent aux 4 champs (login/password) de la fenêtre.
        public string SignInId_Result
        {
            get { return SignInId.Text; }
            set { SignInId.Text = value; }
        }

        public string SignInPw_Result
        {
            get { return SignInPw.Text; }
            set { SignInPw.Text = value; }
        }

        public string SignUpId_Result
        {
            get { return SignUpId.Text; }
            set { SignUpId.Text = value; }
        }

        public string SignUpPw_Result
        {
            get { return SignUpPw.Text; }
            set { SignUpPw.Text = value; }
        }

        /// <summary>
        /// Quand l'utilisateur cherche à se connecter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SignIn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Exit = false;
            SignIn = true;
        }

        /// <summary>
        /// Quand l'utilisateur cherche à s'inscrire.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SignUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Exit = false;
            SignIn = false;
        }

        /// <summary>
        /// Quand l'utilisateur appuie sur la croix rouge pour fermer la fenêtre.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
            Exit = true;
        }
    }
}
