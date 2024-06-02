using Amazon.DynamoDBv2.DataModel;

namespace backend_api_carrinho.Models;

[DynamoDBTable("TCC_TranscricoesDoUsuario")]
public class Transcricoes
{
    [DynamoDBHashKey]
    public string Email { get; set; }
    public List<string> IdsTranscricoes { get; set; }
}