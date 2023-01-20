using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection.PortableExecutable;
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
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
namespace ADO_NET_HW4_BookCatalog;

public partial class MainWindow : Window
{
    Notifier notifier = new Notifier(cfg =>
    {
        cfg.PositionProvider = new WindowPositionProvider(
            parentWindow: Application.Current.MainWindow,
            corner: Corner.TopRight,
            offsetX: 10,
            offsetY: 10);

        cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
            notificationLifetime: TimeSpan.FromSeconds(2),
            maximumNotificationCount: MaximumNotificationCount.FromCount(5));

        cfg.Dispatcher = Application.Current.Dispatcher;
    });


    SqlConnection? connection = null;
   
    DataTable? table = null;
    public MainWindow()
    {
        InitializeComponent();

        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        connection = new SqlConnection();
        connection.ConnectionString = configuration.GetConnectionString("ConStrLib");
       
        table = new();

        table.Columns.Add("Id");
        table.Columns.Add("Name");
        table.Columns.Add("Pages");
        table.Columns.Add("YearPress");
        table.Columns.Add("Id_Author");
        table.Columns.Add("Id_Themes");
        table.Columns.Add("Id_Category");
        table.Columns.Add("Id_Press");
        table.Columns.Add("Comment");
        table.Columns.Add("Quantity");

    }
    DataTable authorsTable = new DataTable();
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SqlDataReader? reader = null;

        try
        {
            connection?.Open();

            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:03';SELECT Id,FirstName +' '+ LastName  FROM Authors;", connection);
            reader = await command.ExecuteReaderAsync();
            int line = 0;

            do
            {
                while (reader.Read())
                {
                    if (line == 0)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            authorsTable.Columns.Add(reader.GetName(i));
                        }
                        line++;
                    }
                    DataRow row = authorsTable.NewRow();

                    for (int i = 0; i < reader.FieldCount; i++)
                        row[i] = reader[i];

                    authorsTable.Rows.Add(row);
                }
            } while (reader.NextResult());

            Author_Cbox.DataContext = authorsTable;
            Author_Cbox.DisplayMemberPath = authorsTable.Columns[1].ToString();
            
            notifier.ShowSuccess("Authors Loaded");

        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            connection?.Close();
            reader?.Close();
        }
    }

    private async void Author_Cbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (connection?.State is ConnectionState.Open)
            return;
        if (!Category_Cbox.IsEnabled)
            Category_Cbox.IsEnabled = !Category_Cbox.IsEnabled;

        DataRowView authorView = Author_Cbox.SelectedItem as DataRowView;
        string id = authorView.Row["Id"] as string;

  

        Category_Cbox.Items.Clear();

        SqlDataReader? reader = null;

        try
        {
            connection?.Open();

         
            if (id is null)
                return;
            
            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:02';SELECT DISTINCT Categories.[Name] FROM Categories\r\nJOIN Books ON Id_Category = Categories.Id\r\nJOIN Authors ON Id_Author = Authors.Id\r\nWHERE Authors.Id =@id", connection);
            command.Parameters.AddWithValue("@id", id);
            
            reader = await command.ExecuteReaderAsync();

            while (reader.Read())
                Category_Cbox.Items.Add(reader["Name"] as string);

            notifier.ShowSuccess("Categories Loaded");


        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            connection?.Close();
            reader?.Close();
        }
    }

    private async void Category_Cbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Category_Cbox.Items.IsEmpty||connection?.State is ConnectionState.Open)
            return;

        SqlDataReader? result = null;

        try
        {
            connection?.Open();

            DataRowView authorView = Author_Cbox.SelectedItem as DataRowView;
            string id = authorView.Row["Id"] as string;

            var name = Category_Cbox.SelectedItem.ToString();

            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:02';SELECT * FROM Books\r\nJOIN Categories ON Categories.Id = Id_Category \r\nJOIN Authors ON Authors.Id = Id_Author \r\nWHERE Categories.Name =@name AND Id_Author =@id\r\n", connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@id", id);

            result = await command.ExecuteReaderAsync();
            table?.Rows.Clear();

            while (result.Read())
            {
                var row = table?.NewRow();

                if (row != null)
                {
                    row["Id"] = result["Id"];
                    row["Name"] = result["Name"];
                    row["Pages"] = result["Pages"];
                    row["YearPress"] = result["YearPress"];
                    row["Id_Author"] = result["Id_Author"];
                    row["Id_Themes"] = result["Id_Themes"];
                    row["Id_Category"] = result["Id_Category"];
                    row["Id_Press"] = result["Id_Press"];
                    row["Comment"] = result["Comment"];
                    row["Quantity"] = result["Quantity"];

                    table?.Rows.Add(row);
                }
            }

           
            ListBooks.ItemsSource = table?.AsDataView();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            connection?.Close();
            result?.Close();
        }
    }

    private async void Btn_Search_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Txt_Search.Text) || connection?.State is ConnectionState.Open)
            return;

        SqlDataReader? result = null;

        table?.Rows.Clear();

        try
        {
            connection?.Open();

            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:02';SELECT * FROM Books\r\nWHERE (Name LIKE @text)", connection);
            command.Parameters.AddWithValue("@text", "%" + Txt_Search.Text + "%");
            result = await command.ExecuteReaderAsync();


            while (result.Read())
            {
                var row = table?.NewRow();

                if (row != null)
                {
                    row["Id"] = result["Id"];
                    row["Name"] = result["Name"];
                    row["Pages"] = result["Pages"];
                    row["YearPress"] = result["YearPress"];
                    row["Id_Author"] = result["Id_Author"];
                    row["Id_Themes"] = result["Id_Themes"];
                    row["Id_Category"] = result["Id_Category"];
                    row["Id_Press"] = result["Id_Press"];
                    row["Comment"] = result["Comment"];
                    row["Quantity"] = result["Quantity"];

                    table?.Rows.Add(row);
                }

            }
            
            ListBooks.ItemsSource = table?.AsDataView();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            connection?.Close();
            result?.Close();
        }
    }
    private void Txt_Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Btn_Search_Click(sender, e);
    }
}
