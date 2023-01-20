using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
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

namespace ADO_NET_HW4_BookCatalog_Classic;
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



    DataTable? table = null;
    DataTable authorsTable = null;
    string? conStr = null;
    public MainWindow()
    {
        InitializeComponent();

        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        conStr = configuration.GetConnectionString("ConStrLib");
        table = new();
        authorsTable = new DataTable();

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
    private  void Window_Loaded(object sender, RoutedEventArgs e)
    {


        try
        {
            var connection = new SqlConnection(conStr);
            connection.Open();
            AsyncCallback callback = new AsyncCallback(CallBackAuthorLoaded);
            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:03';SELECT Id,FirstName +' '+ LastName  FROM Authors;", connection);
            command.BeginExecuteReader(callback,command);


        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

    }



    private void CallBackAuthorLoaded(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);

                var dataTable = new DataTable();


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
               
                
                Dispatcher.Invoke(() => Author_Cbox.DataContext = authorsTable);
                Dispatcher.Invoke(() => Author_Cbox.DisplayMemberPath =  authorsTable.Columns[1].ToString());
                Dispatcher.Invoke(() => notifier.ShowSuccess("Authors Loaded"));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
    }




    private  void Author_Cbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (!Category_Cbox.IsEnabled)
            Category_Cbox.IsEnabled = !Category_Cbox.IsEnabled;

        DataRowView authorView = Author_Cbox.SelectedItem as DataRowView;
        string id = authorView.Row["Id"] as string;


        if (id is null)
            return;

        Category_Cbox.Items.Clear();

       

        try
        {
            var connection = new SqlConnection(conStr);

            connection?.Open();
            AsyncCallback callback = new AsyncCallback(CallBackCmbAuthors);

            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:02';SELECT DISTINCT Categories.[Name] FROM Categories\r\nJOIN Books ON Id_Category = Categories.Id\r\nJOIN Authors ON Id_Author = Authors.Id\r\nWHERE Authors.Id =@id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.BeginExecuteReader(callback, command);
 
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }



    private void CallBackCmbAuthors(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);

                while (reader.Read())
                    Dispatcher.Invoke(() => Category_Cbox.Items.Add(reader["Name"] as string));
                Dispatcher.Invoke(() => notifier.ShowSuccess("Categories Loaded"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
    }




    private void CallBackCmbCategories(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);
                table?.Rows.Clear();
                while (reader.Read())
                {
                    var row = table?.NewRow();

                    if (row != null)
                    {
                        row["Id"] = reader["Id"];
                        row["Name"] = reader["Name"];
                        row["Pages"] = reader["Pages"];
                        row["YearPress"] = reader["YearPress"];
                        row["Id_Author"] = reader["Id_Author"];
                        row["Id_Themes"] = reader["Id_Themes"];
                        row["Id_Category"] = reader["Id_Category"];
                        row["Id_Press"] = reader["Id_Press"];
                        row["Comment"] = reader["Comment"];
                        row["Quantity"] = reader["Quantity"];

                        table?.Rows.Add(row);
                    }
                }

                Dispatcher.Invoke(() => ListBooks.ItemsSource = table?.AsDataView());
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
    }
    private void Category_Cbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Category_Cbox.Items.IsEmpty)
            return;

        try
        {
            var connection = new SqlConnection(conStr);
            connection?.Open();

            DataRowView authorView = Author_Cbox.SelectedItem as DataRowView;
            string id = authorView.Row["Id"] as string;

            var name = Category_Cbox.SelectedItem.ToString();
            AsyncCallback callback = new AsyncCallback(CallBackCmbCategories);
            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:02';SELECT * FROM Books\r\nJOIN Categories ON Categories.Id = Id_Category \r\nJOIN Authors ON Authors.Id = Id_Author \r\nWHERE Categories.Name =@name AND Id_Author =@id\r\n", connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@id", id);
            command.BeginExecuteReader(callback, command);


        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

    }


    private void CallBackBtnSearch(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);

                while (reader.Read())
                {
                    var row = table?.NewRow();

                    if (row != null)
                    {
                        row["Id"] = reader["Id"];
                        row["Name"] = reader["Name"];
                        row["Pages"] = reader["Pages"];
                        row["YearPress"] = reader["YearPress"];
                        row["Id_Author"] = reader["Id_Author"];
                        row["Id_Themes"] = reader["Id_Themes"];
                        row["Id_Category"] = reader["Id_Category"];
                        row["Id_Press"] = reader["Id_Press"];
                        row["Comment"] = reader["Comment"];
                        row["Quantity"] = reader["Quantity"];

                        table?.Rows.Add(row);
                    }

                }
                Dispatcher.Invoke(() => ListBooks.ItemsSource = table?.AsDataView());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
    }

    private async void Btn_Search_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Txt_Search.Text))
            return;


        table?.Rows.Clear();

        try
        {
            var connection = new SqlConnection(conStr);
            connection?.Open();
            AsyncCallback callback = new AsyncCallback(CallBackBtnSearch);
            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:02';SELECT * FROM Books\r\nWHERE (Name LIKE @text)", connection);
            command.Parameters.AddWithValue("@text", "%" + Txt_Search.Text + "%");

            command.BeginExecuteReader(callback, command);

          

          
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
  
    }

    private void Txt_Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Btn_Search_Click(sender, e);
    }
}