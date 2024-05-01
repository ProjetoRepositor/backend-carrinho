namespace backend_api_carrinho.Models;

public class ExportObject
{
    public DateTime ExportDate { get; set; }
    public string Url { get; set; }
    
    public int QuantidadeProdutos { get; set; }
}