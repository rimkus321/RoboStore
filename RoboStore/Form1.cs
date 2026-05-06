using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace RoboStore
{
    public partial class Form1 : Form
    {
        private DataGridView dgvRobots;
        private DataGridView dgvLogs;

        private TextBox txtModel;
        private TextBox txtPrice;
        private TextBox txtQuantity;
        private ComboBox cmbType;

        private Button btnAddRobot;
        private Button btnOrder;
        private Button btnBackup;
        private Button btnRestore;

        private readonly string connectionString = @"Server=.\SQLEXPRESS01;Database=RoboStore;Trusted_Connection=True;";

        private readonly string backupPath = @"C:\Backup\RoboStore.bak";
        private readonly string offlineLogFile = @"C:\Backup\offline_critical.csv";

        private string currentUser;
        private string currentRole;

        public Form1(string user, string role)
        {
            currentUser = user;
            currentRole = role;

            Directory.CreateDirectory(@"C:\Backup");

            Width = 1000;
            Height = 600;

            InitUI();
            ConfigureAccess();

            LoadRobots();
            LoadLogs();
        }

        private void InitUI()
        {
            dgvRobots = new DataGridView { Top = 10, Left = 10, Width = 600, Height = 200 };
            dgvLogs = new DataGridView { Top = 220, Left = 10, Width = 600, Height = 200 };

            txtModel = new TextBox { Top = 10, Left = 650, Width = 200 };
            cmbType = new ComboBox { Top = 40, Left = 650, Width = 200 };
            cmbType.Items.AddRange(new[] { "Бытовой", "Промышленный" });

            txtPrice = new TextBox { Top = 70, Left = 650, Width = 200 };
            txtQuantity = new TextBox { Top = 100, Left = 650, Width = 200 };

            btnAddRobot = new Button { Top = 130, Left = 650, Width = 200, Text = "Добавить" };
            btnOrder = new Button { Top = 160, Left = 650, Width = 200, Text = "Продать" };
            btnBackup = new Button { Top = 190, Left = 650, Width = 200, Text = "Backup" };
            btnRestore = new Button { Top = 220, Left = 650, Width = 200, Text = "Restore" };

            btnAddRobot.Click += AddRobot;
            btnOrder.Click += CreateOrder;
            btnBackup.Click += BackupDb;
            btnRestore.Click += RestoreDb;

            Controls.AddRange(new Control[]
            {
                dgvRobots, dgvLogs,
                txtModel, cmbType, txtPrice, txtQuantity,
                btnAddRobot, btnOrder, btnBackup, btnRestore
            });
        }

        private void ConfigureAccess()
        {
            if (currentRole == "manager")
            {
                btnAddRobot.Enabled = false;
                btnBackup.Enabled = false;
                btnRestore.Enabled = false;
            }
        }

        private void LoadRobots()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var da = new SqlDataAdapter("SELECT * FROM Robots", conn);
                    var dt = new DataTable();
                    da.Fill(dt);
                    dgvRobots.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(offlineLogFile, FormatOfflineLog("system", "LoadRobots error", ex.Message));
            }
        }

        private void LoadLogs()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var da = new SqlDataAdapter("SELECT * FROM Logs ORDER BY LogDate DESC", conn);
                    var dt = new DataTable();
                    da.Fill(dt);
                    dgvLogs.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(offlineLogFile, FormatOfflineLog("system", "LoadLogs error", ex.Message));
            }
        }

        private void AddRobot(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var cmd = new SqlCommand(
                        "INSERT INTO Robots (Model, Type, Price, Quantity) VALUES (@m,@t,@p,@q)", conn);

                    cmd.Parameters.AddWithValue("@m", txtModel.Text);
                    cmd.Parameters.AddWithValue("@t", cmbType.Text);
                    cmd.Parameters.AddWithValue("@p", decimal.Parse(txtPrice.Text));
                    cmd.Parameters.AddWithValue("@q", int.Parse(txtQuantity.Text));

                    cmd.ExecuteNonQuery();
                }

                LogAction(currentUser, "Добавление", txtModel.Text);
                LoadRobots();
            }
            catch (Exception ex)
            {
                File.AppendAllText(offlineLogFile, FormatOfflineLog(currentUser, "AddRobot error", ex.Message));
            }
        }

        private void CreateOrder(object sender, EventArgs e)
        {
            try
            {
                if (dgvRobots.CurrentRow == null) return;

                int id = Convert.ToInt32(dgvRobots.CurrentRow.Cells["Id"].Value);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // уменьшаем количество
                    var updateCmd = new SqlCommand(
                        "UPDATE Robots SET Quantity = Quantity - 1 WHERE Id = @id AND Quantity > 0", conn);
                    updateCmd.Parameters.AddWithValue("@id", id);

                    int affected = updateCmd.ExecuteNonQuery();

                    if (affected == 0)
                    {
                        MessageBox.Show("Нет доступного количества для продажи");
                        return;
                    }

                    // создаём заказ
                    var insertCmd = new SqlCommand(
                        "INSERT INTO Orders (RobotId, PaymentStatus) VALUES (@id,'Оплачен')", conn);

                    insertCmd.Parameters.AddWithValue("@id", id);
                    insertCmd.ExecuteNonQuery();
                }

                LogAction(currentUser, "Продажа", $"Robot {id}");
                LoadRobots(); // обязательно обновляем UI
            }
            catch (Exception ex)
            {
                File.AppendAllText(offlineLogFile,
                    FormatOfflineLog(currentUser, "CreateOrder error", ex.Message));
            }
        }

        private void BackupDb(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    new SqlCommand($"BACKUP DATABASE RoboStore TO DISK='{backupPath}' WITH INIT", conn)
                        .ExecuteNonQuery();
                }

                LogAction(currentUser, "Backup", "OK");
                MessageBox.Show("Backup OK");
            }
            catch (Exception ex)
            {
                File.AppendAllText(offlineLogFile, FormatOfflineLog(currentUser, "Backup error", ex.Message));
            }
        }

        private void RestoreDb(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(
                    @"Server=.\SQLEXPRESS01;Database=master;Trusted_Connection=True;"))
                {
                    conn.Open();

                    var cmd = new SqlCommand($@"
ALTER DATABASE RoboStore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE RoboStore 
FROM DISK = '{backupPath}' 
WITH REPLACE;

ALTER DATABASE RoboStore SET MULTI_USER;", conn);

                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                }

                LogAction(currentUser, "Restore", "OK");
                LoadRobots();
                MessageBox.Show("Restore OK");
            }
            catch (Exception ex)
            {
                MessageBox.Show("RESTORE ERROR: " + ex.Message);

                File.AppendAllText(offlineLogFile,
                    FormatOfflineLog(currentUser, "Restore error", ex.Message));
            }
        }

        private void LogAction(string user, string action, string desc)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "INSERT INTO Logs (Username, ActionType, Description) VALUES (@u,@a,@d)", conn);

                    cmd.Parameters.AddWithValue("@u", user);
                    cmd.Parameters.AddWithValue("@a", action);
                    cmd.Parameters.AddWithValue("@d", desc);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(offlineLogFile, FormatOfflineLog(user, action, ex.Message));
            }
        }

        private string FormatOfflineLog(string u, string a, string d)
        {
            return $"{DateTime.Now};{u};{a};{d}{Environment.NewLine}";
        }
    }
}