using System;
using System.Windows.Forms;

namespace RoboStore
{
    public partial class FormLogin : Form
    {
        private TextBox txtLogin;
        private TextBox txtPassword;
        private Button btnLogin;

        public string UserRole { get; private set; }
        public string Username { get; private set; }

        public FormLogin()
        {
            Width = 300;
            Height = 200;

            txtLogin = new TextBox { Top = 20, Left = 50, Width = 200 };
            txtPassword = new TextBox { Top = 60, Left = 50, Width = 200, PasswordChar = '*' };
            btnLogin = new Button { Top = 100, Left = 50, Width = 200, Text = "Login" };

            btnLogin.Click += btnLogin_Click;

            Controls.Add(txtLogin);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtLogin.Text == "admin" && txtPassword.Text == "admin")
            {
                Username = "admin";
                UserRole = "admin";
                DialogResult = DialogResult.OK;
            }
            else if (txtLogin.Text == "manager" && txtPassword.Text == "manager")
            {
                Username = "manager";
                UserRole = "manager";
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Неверные данные");
            }
        }
    }
}