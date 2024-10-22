using System;
using System.Data.SqlClient;
using System.Windows.Forms;

public class DatabaseConnection
{
    private string connectionString;

    public DatabaseConnection()
    {
        connectionString = "Server=WIN-NRSBVRMVO1H\\SQLEXPRESS;Database=FastOrder;Trusted_Connection=yes;TrustServerCertificate=True;";
    }

    public SqlConnection OpenConnection()
    {
        SqlConnection connection = new SqlConnection(connectionString);

        try
        {
            connection.Open();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao conectar ao banco de dados: " + ex.Message);
            throw;
        }

        return connection;
    }
}
