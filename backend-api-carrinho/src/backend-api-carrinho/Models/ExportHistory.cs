using Amazon.DynamoDBv2.DataModel;

namespace backend_api_carrinho.Models;

[DynamoDBTable("TCC_Export")]
public class ExportHistory
{
    [DynamoDBHashKey]
    public string Email { get; set; }
    public List<ExportObject> Exports { get; set; }
}