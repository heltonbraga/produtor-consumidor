using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class Basics
    {
        public static string keyPreparador = "preparador";
        public static string keyProcessador = "processador";
        public static string keySolicitante = "solicitante";
        public static string keyCount = "count";
        public static string keyListaInsumos = "insumos";
        public static string keyListaIds = "lista-ids";
        public static string redisServer = "localhost";
        public static string channelName = "sol-pro";
        public static string sqlConnectString = "Host=localhost;Username=postgres;Password=f158887;Database=teste";
    }

    public class Input
    {
        public int Id { get; set; }
        public float ValorUnitario { get; set; }
        public int Quantidade { get; set; }
        public string Cupom { get; set; }
        public Input() {}
        public Input(int id)
        {
            Id = id;
            Cupom = "";
        }

        public Input(int id, float valorUnitario, int quantidade, string cupom)
        {
            Id = id;
            ValorUnitario = valorUnitario;
            Quantidade = quantidade;
            Cupom = cupom;
        }
    }
}
