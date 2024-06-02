using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using backend_api_carrinho.Models;
using Microsoft.AspNetCore.Mvc;

namespace backend_api_carrinho.Controllers;

[Route("api/v1/[controller]")]
public class TranscricoesController: ControllerBase
{
    DynamoDBContext _context;

    public TranscricoesController(IAmazonDynamoDB context)
    {
        _context = new(context);
    }
    
    private async Task<string?> BuscaUsuarioEmail(string token)
    {
        var identificacao = await _context.LoadAsync<Identificacao>(token);

        var email = identificacao?.Email;

        return email;
    }
    
    // [HttpGet]
    // public async Task<IActionResult> GetStatus([FromHeader] string token)
    // {
    //     var email = await BuscaUsuarioEmail(token);
    //
    //     if (email is null)
    //     {
    //         return Unauthorized();
    //     }
    //     
    //     var response = await _context.LoadAsync<Transcricoes>(email);
    //     return Ok(response);
    // }
    
    [HttpPost]
    public async Task<IActionResult> PostStatus([FromHeader] string token, [FromBody] TranscricaoRequest transcricao)
    {
        var email = await BuscaUsuarioEmail(token);

        Console.WriteLine(email);
        Console.WriteLine(transcricao);
        Console.WriteLine(token);
        
        if (email is null)
        {
            return Unauthorized();
        }
        
        var response = (await _context.LoadAsync<Transcricoes>(email)) ?? new Transcricoes
        {
            Email = email,
            IdsTranscricoes = new(),
        };
        
        if (!response.IdsTranscricoes.Contains(transcricao.IdTranscricao))
            response.IdsTranscricoes.Add(transcricao.IdTranscricao);

        await _context.SaveAsync(response);
        
        return Ok(response);
    }
}