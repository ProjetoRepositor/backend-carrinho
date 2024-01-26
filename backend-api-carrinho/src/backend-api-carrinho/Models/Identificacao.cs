using Amazon.DynamoDBv2.DataModel;

namespace backend_api_carrinho.Models;

[DynamoDBTable("TCC_Autorizacao")]
public class Identificacao
{
    [DynamoDBHashKey]
    public string Token { get; set; }
    public string Email { get; set; }
}