using System;
using System.IO;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace PrimoAutoEletrica.Views
{
    public partial class DatabaseConfigurationWindow : Window
    {
        private readonly string _configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Config", "database.json");

        public DatabaseConfigurationWindow()
        {
            InitializeComponent();
            try
            {
                var path = Path.GetFullPath(_configPath);
                if (File.Exists(path)) ConnectionStringBox.Text = File.ReadAllText(path);
            }
            catch { }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var cs = ConnectionStringBox.Text.Trim();
            try
            {
                using var conn = new SqlConnection(cs);
                conn.Open();
                StatusText.Text = $"Conectado: {conn.DataSource} / {conn.Database}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Falha: " + ex.Message;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dir = Path.GetDirectoryName(_configPath)!;
                Directory.CreateDirectory(Path.GetFullPath(dir));
                File.WriteAllText(Path.GetFullPath(_configPath), ConnectionStringBox.Text);
                StatusText.Text = "Salvo com sucesso.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Erro ao salvar: " + ex.Message;
            }
        }
    }
}
