using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using backend_api_carrinho.Models;
using Microsoft.AspNetCore.Mvc;

namespace backend_api_carrinho.Controllers;

[Route("api/v1/[controller]")]
public class CarrinhoController : ControllerBase
{
    DynamoDBContext _context;
    
    public CarrinhoController(IAmazonDynamoDB context)
    {
        this._context = new (context);
    }

    private async Task<string> BuscaUsuarioId(string token)
    {
        return token;
    }

    private async Task<Carrinho?> LerCarrinhoAtual(string usuarioId)
    {
        var scanConditions = new List<ScanCondition>
        {
            new("UsuarioId", ScanOperator.Equal, usuarioId),
        };
        var result = await _context.ScanAsync<Carrinho>(scanConditions).GetRemainingAsync();

        if (result == null || result.Count == 0)
        {
            return null;
        }

        var response = result.OrderByDescending(r => r.CriadoEm).ToList()[0];

        response.Produtos = response.Produtos.Where(p => p.Quantidade > 0).ToList();

        return response;
    }

    [HttpPost]
    public async Task<IActionResult> AdicionarProduto([FromHeader] string token, Produto novoProduto)
    {
        var usuarioId = await BuscaUsuarioId(token);
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
            produto.Quantidade += produto.Quantidade;
        }

        await _context.SaveAsync(carrinho);
        
        var novoCarrinho = await LerCarrinhoAtual(usuarioId);
        
        return novoCarrinho is null ? Problem() : Ok(novoCarrinho.Produtos);
    }

    [HttpGet]
    public async Task<IActionResult> LerCarrinho([FromHeader] string token)
    {
        var usuarioId = await BuscaUsuarioId(token);
        var carrinho = await LerCarrinhoAtual(usuarioId);
        
        return carrinho is null ? Unauthorized() : Ok(carrinho.Produtos);
    }
    
    [HttpDelete]
    public async Task<IActionResult> RemoverProduto([FromHeader] string token, Produto produtoRemovido)
    {
        var usuarioId = await BuscaUsuarioId(token);
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
        
        var scanConditions = new List<ScanCondition>
        {
            new("UsuarioId", ScanOperator.Equal, usuarioId),
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