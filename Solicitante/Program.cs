using Npgsql;
using StackExchange.Redis;
using Newtonsoft.Json;
using Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System.Diagnostics;
using System.Threading;

namespace Solicitante
{

    class Program
    {

        static Task Main() => Solicitar();

        static async Task Solicitar()
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Inicializando nó solicitante");
            ConnectionMultiplexer conexaoRedis = ConnectionMultiplexer.Connect(Basics.redisServer);
            var dbRedis = conexaoRedis.GetDatabase();
            var pub = conexaoRedis.GetSubscriber();
            //dbRedis.StringSetAsync(Basics.keySolicitante, "0", expiry: null);
            //dbRedis.StringSetAsync(Basics.keyProcessador, "0", expiry: null);
            using var conn = new NpgsqlConnection(Basics.sqlConnectString);
            conn.Open();
            var preparador = dbRedis.StringGet(Basics.keyPreparador);
            while (preparador == RedisValue.Null || Int32.Parse(preparador) == 0)
            {
                logger.Info("Esperando nó preparador");
                Thread.Sleep(100);
                preparador = dbRedis.StringGet(Basics.keyProcessador);
            }
            int total = Int32.Parse(preparador);
            logger.Info(total + " preparadas");
            int solicitadas = 0;
            int fila = 0;
            while(solicitadas < total)
            {
                var id = dbRedis.ListLeftPop(Basics.keyListaIds);
                if(id == RedisValue.Null)
                {
                    Thread.Sleep(100);
                    var opt = dbRedis.StringGet(Basics.keySolicitante);
                    solicitadas = opt == RedisValue.Null ? 0 : Int32.Parse(opt);
                    continue;
                }
                //
                await dbRedis.ListRightPushAsync(
                    Basics.keyListaInsumos,
                    SelectInput(id, conn)).ContinueWith(delegate
                    {
                        pub.PublishAsync(Basics.channelName, "go");
                    });
                solicitadas = (int)dbRedis.StringIncrement(Basics.keySolicitante, 1);
                //
                fila = (int)dbRedis.ListLength(Basics.keyListaInsumos);
                var qp = dbRedis.StringGet(Basics.keyProcessador);
                qp = qp == RedisValue.Null ? 0 : Int32.Parse(qp);
                if (fila == 100 && qp == 0)
                {
                    logger.Info("100 solicitações enfileiradas e ainda não há nó processador.");
                }
                if (fila > 5000)
                {
                    logger.Info("Mais de 5000 solicitações enfileiradas, pausando nó solicitante por 1 segundo.");
                    Thread.Sleep(1000);
                }
            }

            var channel = await pub.SubscribeAsync(Basics.channelName);
            channel.OnMessage(msg =>
            {
                fila = (int)dbRedis.ListLength(Basics.keyListaInsumos);
                if (fila == 0)
                {
                    logger.Info("Fila zerada.");
                    channel.Unsubscribe();
                    pub.Publish(Basics.channelName, "stop");
                }
            });

            conn.Close();
            conn.Dispose();
            conexaoRedis.Dispose();

            logger.Info("Finalizando nó solicitante");
            Environment.Exit(0);
        }

        /*
         * Busca entradas necessárias para o processamento do item
         */
        private static string SelectInput(string id, NpgsqlConnection conn)
        {
            using var cmd = new NpgsqlCommand("SELECT id, val_und, qtd, cupom FROM entrada where id = " + id, conn);
            using var reader = cmd.ExecuteReader();
            Input input;
            if (reader.Read())
            {
                input = new Input(
                    reader.GetInt32(0),
                    reader.GetFloat(1),
                    reader.GetInt32(2),
                    reader.GetString(3));
            }
            else
            {
                input = new Input(Int32.Parse(id));
            }
            return JsonConvert.SerializeObject(input);
        }

    }
}