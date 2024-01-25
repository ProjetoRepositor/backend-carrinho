using Amazon.DynamoDBv2.DataModel;

namespace backend_api_carrinho.Models;

[DynamoDBTable("TCC_Carrinho")]
public class Carrinho
{
    [DynamoDBHashKey]
    public string CarrinhoId { get; set; }
    [DynamoDBRangeKey]
    public string UsuarioId { get; set; }
    public List<Produto> Produtos { get; set; }
    public DateTime CriadoEm { get; set; }
}