using System;
using System.Windows.Forms;

namespace RoboStore
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FormLogin login = new FormLogin();

            if (login.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new Form1(login.Username, login.UserRole));
            }
        }
    }
}