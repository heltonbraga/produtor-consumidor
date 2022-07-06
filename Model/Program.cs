using StackExchange.Redis;
using static Model.Basics;
using System.Diagnostics;

Console.WriteLine("-Monitoramento-");
var timer = new Stopwatch();
timer.Start();
ConnectionMultiplexer conexaoRedis = ConnectionMultiplexer.Connect(redisServer);
var dbRedis = conexaoRedis.GetDatabase();
var pos = Console.GetCursorPosition();
Console.WriteLine("...");
double rep = 0;
int max = 50;
while (true)
{
    var total = (int)dbRedis.StringGet(keyPreparador);
    var proc = (int)dbRedis.StringGet(keyProcessador);
    var sol = (int)dbRedis.StringGet(keySolicitante);
    var len = (int)dbRedis.ListLength(keyListaInsumos);
    var qtd = (int)dbRedis.StringGet(keyCount);
    if (total == 0)
    {
        continue;
    }
    Console.SetCursorPosition(0, pos.Top);
    Console.WriteLine("Total: " + new string('#', max) + " " + total);
    rep = Math.Ceiling((double)(len * max / 5000));
    Console.WriteLine("Fila:  " + new string('#', (int)rep) + " "+ len + new string(' ', (int)(80-rep)));
    rep = Math.Ceiling((double)(sol * max / total));
    Console.WriteLine("Solicitadas:  " + new string('#', (int)rep) + " " + sol);
    rep = Math.Ceiling((double)(qtd * max / total));
    Console.WriteLine("Calculadas: " + new string('#', (int)rep) + " " + qtd);
    Console.WriteLine("Processadores ativos: " + proc);
    Console.WriteLine("Tempo de execução: " + timer.Elapsed.TotalSeconds);
    if (total == qtd)
    {
        break;
    }
    Thread.Sleep(100);
}

timer.Stop();
dbRedis.KeyDeleteAsync(keyPreparador);
dbRedis.KeyDeleteAsync(keyProcessador);
dbRedis.KeyDeleteAsync(keySolicitante);
dbRedis.KeyDeleteAsync(keyCount);
dbRedis.KeyDeleteAsync(keyListaInsumos);
dbRedis.KeyDeleteAsync(keyListaIds);
Console.WriteLine("-Fim-");
Console.ReadLine();