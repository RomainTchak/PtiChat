using System.Windows;

namespace WpfApplication2
{
    partial class MyDialog : Window
    {

        public MyDialog()
        {
            InitializeComponent();
        }

        public bool SignIn { get; private set; }
        public bool Exit { get; private set; }

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

        private void SignIn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Exit = false;
            SignIn = true;
        }

        private void SignUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Exit = false;
            SignIn = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
            Exit = true;
        }
    }
}
