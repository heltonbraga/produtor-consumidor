using Npgsql;
using System.Text;

var sqlConnectString = "Host=localhost;Username=postgres;Password=f158887;Database=teste";
using var conn = new NpgsqlConnection(sqlConnectString);
conn.Open();

Console.WriteLine("popular banco de dados...");

var sb = "INSERT INTO entrada (val_und, qtd, cupom, produto) VALUES (@val, @qtd, @cpm, @prd)";
Random rnd = new Random();
for (var i = 0; i < 50000; i++)
{
    using var command = new NpgsqlCommand(connection: conn, cmdText: sb);
    command.Parameters.Add(new NpgsqlParameter<float>("val", rnd.Next(1, 1000) / rnd.Next(1, 10)));
    command.Parameters.Add(new NpgsqlParameter<int>("qtd", rnd.Next(1, 20)));
    command.Parameters.Add(new NpgsqlParameter<string>("cpm", rnd.Next(0,20).ToString("D2")+"desc"));
    command.Parameters.Add(new NpgsqlParameter<int>("prd", rnd.Next(1, 100)));
    await command.ExecuteNonQueryAsync();
}


Console.WriteLine("...terminou!");