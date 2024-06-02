using Amazon.DynamoDBv2.DataModel;

namespace backend_api_carrinho.Models;

[DynamoDBTable("TCC_StatusTranscricao")]
public class Status
{
    [DynamoDBHashKey]
    public string IdTranscricao { get; set; }
    public string SituacaoAtual { get; set; }
    public string? TextoRecebido { get; set; }
    public string? ProdutoEncontrado { get; set; }
    public int? Quantidade { get; set; }
}