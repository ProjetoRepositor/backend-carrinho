using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Transfer;
using backend_api_carrinho.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace backend_api_carrinho.Controllers;

[Route("api/v1/[controller]")]
public class CarrinhoController : ControllerBase
{
    DynamoDBContext _context;
    
    public CarrinhoController(IAmazonDynamoDB context)
    {
        _context = new (context);
    }

    private async Task<string?> BuscaUsuarioId(string token)
    {
        var identificacao = await _context.LoadAsync<Identificacao>(token);

        var email = identificacao?.Email;

        return email;
    }
    
    private string GerarStringAleatoria(int tamanho)
    {
        Random random = new Random();
        const string caracteresPermitidos = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        char[] arrayCaracteres = new char[tamanho];
        for (int i = 0; i < tamanho; i++)
        {
            arrayCaracteres[i] = caracteresPermitidos[random.Next(caracteresPermitidos.Length)];
        }

        return new string(arrayCaracteres);
    }

    private async Task<Carrinho> CriarCarrinho(string UsuarioEmail)
    {
        var novoCarrinho = new Carrinho
        {
            CarrinhoId = GerarStringAleatoria(20),
            UsuarioEmail = UsuarioEmail,
            Produtos = new(),
            CriadoEm = DateTime.Now,
        };
        
        await _context.SaveAsync(novoCarrinho);
        
        return novoCarrinho;
    }

    private async Task<Carrinho?> LerCarrinhoAtual(string usuarioEmail)
    {
        var scanConditions = new List<ScanCondition>
        {
            new("UsuarioEmail", ScanOperator.Equal, usuarioEmail),
        };
        var result = await _context.ScanAsync<Carrinho>(scanConditions).GetRemainingAsync();

        if (result == null || result.Count == 0)
        {
            var carrinhoCriado = await CriarCarrinho(usuarioEmail);
            return carrinhoCriado;
        }

        var response = result.OrderByDescending(r => r.CriadoEm).ToList()[0];

        response.Produtos = response.Produtos.Where(p => p.Quantidade > 0).ToList();

        return response;
    }

    [HttpPost]
    public async Task<IActionResult> AdicionarProduto([FromHeader] string token, [FromBody] Produto novoProduto)
    {
        var usuarioId = await BuscaUsuarioId(token);

        if (usuarioId is null)
        {
            return Unauthorized();
        }
        
        var carrinho = await LerCarrinhoAtual(usuarioId);
        
        if (carrinho is null)
            return Unauthorized();

        var produto = carrinho.Produtos.FirstOrDefault(p => p.CodigoDeBarras == novoProduto.CodigoDeBarras);

        if (produto is null)
        {
            carrinho.Produtos.Add(novoProduto);
        }
        else
        {
            produto.Quantidade += novoProduto.Quantidade;
        }

        await _context.SaveAsync(carrinho);
        
        var novoCarrinho = await LerCarrinhoAtual(usuarioId);
        
        return novoCarrinho is null ? Problem() : Ok(novoCarrinho.Produtos);
    }

    private async Task <ExportHistory> FindExportHistory(string email)
    {
        var result = await _context.LoadAsync<ExportHistory>(email);
        var response = result ?? new ExportHistory
        {
            Email = email,
            Exports = new(),
        };
        return response;
    }

    [HttpGet("export-history")]
    public async Task<IActionResult> GetExportHistory([FromHeader] string? token)
    {
        if (token is null) return Unauthorized();
        
        var usuarioId = await BuscaUsuarioId(token);

        if (usuarioId is null)
        {
            return Unauthorized();
        }
        
        var history = await FindExportHistory(usuarioId);

        return Ok(history);
    }
    
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ExportRequest[] request, [FromHeader] string? token)
    {
        if (token is null) return Unauthorized();
        
        var usuarioId = await BuscaUsuarioId(token);

        if (usuarioId is null)
        {
            return Unauthorized();
        }
        
        var stream = new MemoryStream();

        using var package = new ExcelPackage(stream);
    
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");
        worksheet.Cells["A1"].Value = "Produto";
        worksheet.Cells["B1"].Value = "Quantidade";

        var contador = 1;
    
        foreach (var produto in request)
        {
            contador++;
            worksheet.Cells[$"A{contador}"].Value = produto.Produto;
            worksheet.Cells[$"B{contador}"].Value = produto.Quantidade;
        }

        package.Save();

        stream.Position = 0;

        var s3Client = new AmazonS3Client();

        var bucketName = "imagens.tcc.caioruiz.com";
        var fileName = "exports/" + Guid.NewGuid().ToString() + ".xlsx";

        var fileTransferUtility = new TransferUtility(s3Client);
        await fileTransferUtility.UploadAsync(stream, bucketName, fileName);

        var url = $"https://{bucketName}/{fileName}";

        var history = await FindExportHistory(usuarioId);

        var newObject = new ExportObject
        {
            Url = url,
            ExportDate = DateTime.Now,
            QuantidadeProdutos = request.Length,
        };
        
        history.Exports.Add(newObject);

        await _context.SaveAsync(history);

        return Ok(newObject);
    }
    
    [HttpGet]
    public async Task<IActionResult> LerCarrinho([FromHeader] string token)
    {
        var usuarioId = await BuscaUsuarioId(token);

        if (usuarioId is null)
        {
            return Unauthorized();
        }
        
        var carrinho = await LerCarrinhoAtual(usuarioId);
        
        return carrinho is null ? Unauthorized() : Ok(carrinho.Produtos);
    }
    
    [HttpDelete]
    public async Task<IActionResult> RemoverProduto([FromHeader] string token, [FromBody] Produto produtoRemovido)
    {
        var usuarioId = await BuscaUsuarioId(token);

        if (usuarioId is null)
        {
            return Unauthorized();
        }
        
        var carrinho = await LerCarrinhoAtual(usuarioId);
        
        if (carrinho is null)
            return Unauthorized();

        var produto = carrinho.Produtos.FirstOrDefault(p => p.CodigoDeBarras == produtoRemovido.CodigoDeBarras);

        if (produto is null || produto.Quantidade == 0)
        {
            return NotFound("Produto não está no carrinho");
        }
        
        produto.Quantidade = Math.Max(produto.Quantidade - produtoRemovido.Quantidade, 0);

        await _context.SaveAsync(carrinho);
        
        var novoCarrinho = await LerCarrinhoAtual(usuarioId);

        return novoCarrinho is null ? Problem() : Ok(new { Produtos = novoCarrinho.Produtos });
    }

    [HttpGet("Historico")]
    public async Task<IActionResult> VerHistorico([FromHeader] string token)
    {
        var usuarioId = await BuscaUsuarioId(token);

        if (usuarioId is null)
        {
            return Unauthorized();
        }
        
        var scanConditions = new List<ScanCondition>
        {
            new("UsuarioEmail", ScanOperator.Equal, usuarioId),
        };
        var result = await _context.ScanAsync<Carrinho>(scanConditions).GetRemainingAsync();

        if (result == null || result.Count == 0)
        {
            return NotFound();
        }

        var response = result.OrderByDescending(r => r.CriadoEm).Select(c => c.CarrinhoId).ToList();

        return Ok(new { Carrinhos = response });
    }
}