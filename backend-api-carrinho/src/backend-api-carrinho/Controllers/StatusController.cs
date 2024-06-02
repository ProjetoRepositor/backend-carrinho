using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using backend_api_carrinho.Models;
using Microsoft.AspNetCore.Mvc;

namespace backend_api_carrinho.Controllers;

[Route("api/v1/[controller]")]
public class StatusController : ControllerBase
{
    DynamoDBContext _context;

    public StatusController(IAmazonDynamoDB context)
    {
        _context = new(context);
    }
    
    private async Task<string?> BuscaUsuarioEmail(string token)
    {
        var identificacao = await _context.LoadAsync<Identificacao>(token);

        var email = identificacao?.Email;

        return email;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStatus([FromHeader] string token)
    {
        var email = await BuscaUsuarioEmail(token);

        if (email is null)
        {
            return Unauthorized();
        }

        var transcricoes = (await _context.LoadAsync<Transcricoes>(email)) ??  new Transcricoes
        {
            Email = email,
            IdsTranscricoes = new(),
        };

        var response = new Status[transcricoes.IdsTranscricoes.Count];

        await Task.WhenAll(
            transcricoes.IdsTranscricoes.Select(
                async (id, selector) =>
                {
                    var status = await _context.LoadAsync<Status>(id);
                    response[selector] = status;
                    Console.WriteLine(id);
                }
            )
        );

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStatus([FromRoute] string id)
    {
        var response = await _context.LoadAsync<Status>(id);
        return Ok(response);
    }

    [HttpPost]
    [HttpPut]
    public async Task<IActionResult> SetStatus([FromBody] Status request)
    {
        await _context.SaveAsync(request);
        return Ok(request);
    }
}