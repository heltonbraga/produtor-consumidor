using Npgsql;
using StackExchange.Redis;
using Newtonsoft.Json;
using Model;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System.Diagnostics;

namespace Processador
{

    class Program
    {

        static Task Main() => Preparar();

        static async Task Preparar()
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Inicializando nó processador");
            using var conn = new NpgsqlConnection(Basics.sqlConnectString);
            conn.Open();
            ConnectionMultiplexer conexaoRedis = ConnectionMultiplexer.Connect(Basics.redisServer);
            var dbRedis = conexaoRedis.GetDatabase();
            var channel = conexaoRedis.GetSubscriber().Subscribe(Basics.channelName);
            if(dbRedis.StringGet(Basics.keyProcessador) == RedisValue.Null)
            {
                dbRedis.StringSet(Basics.keyProcessador, "0", expiry: null);
            }
            dbRedis.StringIncrementAsync(Basics.keyProcessador, 1);
            logger.Info("Processador aguardando mensagens no canal: "+ Basics.channelName);
            var timer = new Stopwatch();
            while (true)
            {
                if(dbRedis.StringGet(Basics.keySolicitante) == RedisValue.Null)
                {
                    break;
                }
                timer.Restart();
                var cmd = await channel.ReadAsync();
                timer.Stop();
                logger.Info("Esperou mensagem por " + timer.Elapsed.TotalSeconds);
                logger.Info(cmd.ToString());
                if (cmd.ToString().EndsWith("go"))
                {
                    logger.Info("Recebeu mensagem de processamento");
                    timer.Restart();
                    await Processar(conn, logger, conexaoRedis.GetSubscriber(), dbRedis);
                    timer.Stop();
                    logger.Info("Processou continuamente por " + timer.Elapsed.TotalSeconds);

                }
                else if (cmd.ToString().EndsWith("stop"))
                {
                    logger.Info("Recebeu mensagem de encerramento");
                    channel.Unsubscribe();
                    break;
                }
            }
            await conn.CloseAsync();
            conn.Dispose();
            conexaoRedis.Dispose();
            logger.Info("Finalizando nó processador");
            Environment.Exit(0);
        }

        static async Task Processar(NpgsqlConnection conn, Logger logger, ISubscriber pub, IDatabase dbRedis)
        {
            var json = dbRedis.ListLeftPop(Basics.keyListaInsumos);
            while(json != RedisValue.Null)
            {
                var input = JsonConvert.DeserializeObject<Input>(json.ToString());
                logger.Info("Processando "+input.Id);
                InserirResultado(conn, logger, input.Id, Calcular(logger, input));
                dbRedis.StringIncrementAsync(Basics.keyCount, 1);
                pub.PublishAsync(Basics.channelName, "step");
                json = dbRedis.ListLeftPop(Basics.keyListaInsumos);
            }
            pub.PublishAsync(Basics.channelName, "zero");
        }

        private static float Calcular(Logger logger, Input input)
        {
            float resultado = input.ValorUnitario * input.Quantidade;
            var desconto = 0;
            try
            {
                if (input.Quantidade > 9)
                {
                    desconto += 10;
                }
                if (input.Cupom.EndsWith("desc"))
                {
                    desconto += Int32.Parse(input.Cupom.Substring(0, 2));
                }
            } catch(Exception e)
            {
                logger.Error("ERROR: " + input.Id + " -> " + e);
            }
            
            return resultado * (1 - desconto/100);
        }

        static async void InserirResultado(NpgsqlConnection conn, Logger logger, int id, float valor)
        {
            using var cmd = new NpgsqlCommand("INSERT INTO saida (id, valor) values ("+id+", "+valor+")", conn);
            try
            {
                cmd.ExecuteNonQuery();
            } catch(Exception e)
            {
                logger.Error("Falha em [InserirResultado]: " + e.Message);
            }
        }
    }
}