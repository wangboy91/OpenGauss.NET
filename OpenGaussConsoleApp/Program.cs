// See https://aka.ms/new-console-template for more information

using OpenGauss.NET;

Console.WriteLine("Hello, World!");


var connString = "Server=localhost;Port=50432;Username=gaussdb;Password=openGauss@123;Database=testdb;Timeout=60;Command Timeout=60";

var connection = new OpenGaussConnection(connString);
await connection.OpenAsync();
await using var cmd = new OpenGaussCommand("SELECT * FROM public.userinfo", connection);
await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    Console.WriteLine(reader.HasRows);
    Console.WriteLine(reader.FieldCount);
    for (int i = 0; i < reader.FieldCount; i++)
    {
        Console.WriteLine(reader.GetName(i));
        Console.WriteLine(reader.GetValue(i));
    }
}

// var connectString = "Host=localhost;Port=5432;Username=gaussdb;Password=openGauss@123;Database=test;No Reset On Close=true;Maximum Pool Size=512;timeout = 30;";
// using (DataSet dataSet = NpgSqlHelper.ExecuteDataset(connectString, CommandType.Text, "SELECT * FROM public.userinfo"))
// {
//     Console.WriteLine(dataSet.GetXml());
// }