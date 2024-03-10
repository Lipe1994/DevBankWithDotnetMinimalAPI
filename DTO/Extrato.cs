using System.Text.Json.Serialization;

namespace DevBankWithDotnetMinimalAPI.DTO
{
    public struct Extrato
    {
        public Extrato() { }
        public Saldo Saldo { get; set; }

        [JsonPropertyName("ultimas_transacoes")]
        public IEnumerable<TransacaoResumo> Transacoes { get; set; } = new List<TransacaoResumo>();
    }

    public struct Saldo
    {
        public Saldo() { }

        public int Id { get; set; }
        public int Total { get; set; }
        public int Limite { get; set; }
        [JsonPropertyName("data_extrato")]
        public DateTime DataExtrato { get; set; } = DateTime.UtcNow;
    }

    public struct TransacaoResumo
    {
        public int Valor { get; set; }
        public char Tipo { get; set; }
        public required string Descricao { get; set; }
        [JsonPropertyName("realizada_em")]
        public DateTime CriadoEm { get; set; }
    }
}
