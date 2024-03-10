using Dapper;
using DevBankWithDotnet.Repositories;
using DevBankWithDotnet.Repositories.Model;
using DevBankWithDotnetMinimalAPI;
using DevBankWithDotnetMinimalAPI.Command;
using DevBankWithDotnetMinimalAPI.DTO;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseKestrel()
    .ConfigureKestrel(o =>
    {
        //o.Limits.MaxConcurrentConnections = 160;
        o.AddServerHeader = false;
    });


// Add services to the container.
var connection = builder.Configuration.GetConnectionString("DevBank");

builder.Services.AddRequestTimeouts(options => {
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromMilliseconds(4800),
        TimeoutStatusCode = 422
    };
});

builder.Services.AddSingleton<NpgsqlContext>();
//builder.Services.AddSingleton(p => { 
//    var pool = new VirtualPool(p.GetRequiredService<NpgsqlContext>(), p.GetRequiredService<ILogger<VirtualPool>>());
//    pool.StartPool();

//    return pool;
//});

var app = builder.Build();


app.UseErrorHandler();

app.MapGet("clientes/{id:int}/extrato", async ([FromServices] NpgsqlContext context, int id, CancellationToken cancellationToken) =>
{
    using (var connection = await context.Connection.OpenConnectionAsync())
    {
        var sql = @"
SELECT c.Id, c.Total, c.Limite, t.Valor, t.Descricao, t.Tipo, t.CriadoEm
   FROM Cliente c 
   left join Transacao t on t.ClienteId = c.Id 
WHERE c.Id = @Id
ORDER BY t.Id DESC 
LIMIT 10
";
        var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        var transacoes = new List<TransacaoResumo>();
        var saldo = new Saldo();

        bool isFirst = true;
        bool hasExtract = false;
        while (reader.Read())
        {
            if (isFirst)
            {
                saldo.Id = (Int16)reader["Id"];
                saldo.Total = (Int32)reader["Total"];
                saldo.Limite = (Int32)reader["Limite"];
                saldo.DataExtrato = DateTime.Now;
                isFirst = false;
                hasExtract = reader["Valor"] is Int32;
            }

            if (hasExtract)
                transacoes.Add(new TransacaoResumo
                {
                    Valor = (Int32)reader["Valor"],
                    Tipo = ((string)reader["Tipo"])[0],
                    Descricao = (string)reader["Descricao"],
                    CriadoEm = (DateTime)reader["CriadoEm"]
                });
        }

        return Results.Ok(new Extrato()
        {
            Saldo = saldo,
            Transacoes = transacoes,
        });
    }
}).AddEndpointFilter(async (ctx, next) => {

    var id = ctx.GetArgument<int>(1);

    if (id < 1 || id > 5)
    {
        return Results.NotFound();
    }

    return await next(ctx);
});


app.MapPost("clientes/{id:int}/transacoes", async ([FromServices] NpgsqlContext context, int id,[FromBody] TransacaoCommand command, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(command.Descricao) ||
    command.Descricao.Length > 10 ||
    (command.Valor - (int)command.Valor) > 0 ||
    (command.Tipo != 'd' && command.Tipo != 'c'))
    {
        return Results.UnprocessableEntity();
    }

    var novoValor = (int)command.Valor;
    using (var connection = await context.Connection.OpenConnectionAsync())
    {
        Resultado? resultado;
        switch (command.Tipo)
        {
            case 'd':
                resultado = await connection.QueryFirstOrDefaultAsync<Resultado>(
                    new CommandDefinition(@"SELECT _limite Limite, _total Total, LinhaAfetada FROM debitar(@Id, @Valor, @Descricao)",
                        new
                        {
                            Id = id,
                            Valor = novoValor,
                            command.Descricao
                        },
                        cancellationToken: cancellationToken));
                break;
            case 'c':
                resultado = await connection.QueryFirstOrDefaultAsync<Resultado>(
                    new CommandDefinition(@"SELECT _limite Limite, _total Total, LinhaAfetada  FROM creditar(@Id, @Valor, @Descricao)",
                    new
                    {
                        Id = id,
                        Valor = novoValor,
                        command.Descricao
                    },
                    cancellationToken: cancellationToken));
                break;
            default:
                return null;
        }

        if (resultado == null || !resultado.LinhaAfetada)
        {
            return Results.UnprocessableEntity();
        }

        return Results.Ok(resultado);
    }
    
}).AddEndpointFilter(async (ctx, next) => {

    var id = ctx.GetArgument<int>(1);

    if (id < 1 || id > 5)
    {
        return Results.NotFound();
    }

    return await next(ctx);
});

app.Run();
