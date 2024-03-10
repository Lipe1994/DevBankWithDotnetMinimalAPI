using System.Text.Json.Serialization;

namespace DevBankWithDotnet.Repositories.Model;

public class Resultado
{
    public Resultado()
    {  
    }
    public int Limite { get; set; }
    [JsonPropertyName("saldo")]
    public int Total { get; set; }

    [JsonIgnore]
    public bool LinhaAfetada { get; set; }

}
