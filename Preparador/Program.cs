using Npgsql;
using StackExchange.Redis;
using Model;
using NLog;
using System.Diagnostics;

namespace Processador
{

    class Program
    {
        static Task Main() => Preparar();

        static async Task Preparar()
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Inicializando nó preparador");
            ConnectionMultiplexer conexaoRedis = ConnectionMultiplexer.Connect(Basics.redisServer);
            var dbRedis = conexaoRedis.GetDatabase();
            var pub = conexaoRedis.GetSubscriber();
            dbRedis.StringSetAsync(Basics.keyPreparador, "0", expiry: null);
            using var conn = new NpgsqlConnection(Basics.sqlConnectString);
            conn.Open();
            var timer = new Stopwatch();
            timer.Start();
            var ids = SelectIds(logger, conn);
            timer.Stop();
            logger.Info("Tempo para recuperar ids a serem preparados: " + timer.Elapsed.TotalSeconds);
            if (ids.Count() > 0)
            {
                dbRedis.StringIncrementAsync(Basics.keyPreparador, ids.Count());
                ids.ForEach(id => dbRedis.ListRightPushAsync(Basics.keyListaIds, id));
            }
            conn.Close();
            conn.Dispose();
            conexaoRedis.Dispose();
            logger.Info("Finalizando nó preparador");
            Environment.Exit(0);
        }

        /*
        * Busca na base os IDs dos itens a serem processados
        */
        static List<string> SelectIds(Logger logger, NpgsqlConnection conn)
        {
            List<string> ids = new List<string>();
            using var cmd = new NpgsqlCommand("SELECT distinct id FROM entrada", conn);
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                {
                    ids.Add(reader.GetInt32(0).ToString());
                }
            logger.Info(ids.Count() + " ids found.");
            return ids;
        }
    }
}