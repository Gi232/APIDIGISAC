using System;
using System.Net.Http; // import para HttpClient (requisições HTTP)
using System.Net.Http.Headers;
using System.Text.Json;     // import para JsonSerializer (conversão de JSON para objetos C#)
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic; // import para HashSet
using Npgsql; // import para Npgsql (conexão com PostgreSQL)

class Program
{
    static async Task Main(string[] args)
    {
        string url = "https://controlxrm.digisac.me/api/v1/contacts";
        string token = "60b7fa3f351bca976997c61a09aac92c7c838bc5";
        //string connString = "Host=ep-lucky-sun-ayo24u1p-pooler.c-5.us-east-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_aSOJP3H4CYum;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        string connString = "Server= 18.229.25.35; Database=xrm_producao; UID=XRMusuario; PWD=xrm; Connection Timeout=600; TrustServerCertificate=true;";
        string dataInicial = "2026-07-20"; // Data inicial para filtrar os contatos
        string dataFinal = "2026-07-27"; // Data final para filtrar os contatos

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token); // Adiciona o token de autenticação no cabeçalho da requisição

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(); // Abre a conexão com o banco de dados

        //HashSet<string> lojasValidas = new HashSet<string>();

        //string sqlLojas = "SELECT idlojasistema FROM lojas";
        //await using (var cmdLojas = new NpgsqlCommand(sqlLojas, conn))
        //await using (var reader = await cmdLojas.ExecuteReaderAsync())
        //{
        //    while (await reader.ReadAsync())
        //    {
        //        lojasValidas.Add(reader.GetInt32(0).ToString());
        //    }
        //}

        //Console.WriteLine($"Lojas válidas carregadas: {lojasValidas.Count}");

        int pagina = 1;
        int lastPage = 1; // valor "chute" inicial

        while (pagina <= lastPage)
        {
            string urlComPagina = $"{url}?page={pagina}&where[lastMessageAt][$gte]={dataInicial}&where[lastMessageAt][$lte]={dataFinal}";
            HttpResponseMessage resposta = await client.GetAsync(urlComPagina); // faz a requisição GET para a API
            string conteudoJson = await resposta.Content.ReadAsStringAsync();

            RespostaContatos dados = JsonSerializer.Deserialize<RespostaContatos>(conteudoJson);
            lastPage = dados.LastPage; // atualiza com o valor real

            foreach (Contato contato in dados.Data)
            {
                string sql = @"INSERT INTO Contatos_Digisac (id, nome, numero, note, internalname, lastmessageat, createdat) 
                               VALUES (@id, @nome, @numero, @note, @internalname, @lastmessageat, @createdat) 
                               ON CONFLICT (id) DO UPDATE SET
                                   nome = EXCLUDED.nome,
                                   numero = EXCLUDED.numero,
                                   note = EXCLUDED.note,
                                   internalname = EXCLUDED.internalname,
                                   lastmessageat = EXCLUDED.lastmessageat,
                                   createdat = EXCLUDED.createdat";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", contato.Id);
                cmd.Parameters.AddWithValue("nome", (object)contato.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("numero", (object)contato.Data?.Number ?? DBNull.Value);
                cmd.Parameters.AddWithValue("note", (object)contato.Note ?? DBNull.Value);
                cmd.Parameters.AddWithValue("lastmessageat", (object)contato.LastMessageAt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("internalname", (object)contato.InternalName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("createdat", (object)contato.CreatedAt ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                // A PARTE 2 ENTRA AQUI, ainda dentro do foreach:
                if (!string.IsNullOrEmpty(contato.Note))
                {
                    string[] idsLoja = contato.Note.Split('|');

                    foreach (string pedaco in idsLoja)
                    {
                        string idLimpo = pedaco.Trim();

                        if (lojasValidas.Contains(idLimpo))
                        {
                            string sqlLigacao = "INSERT INTO Contatos_Digisac_Loja (idcontato, idlojasistema) VALUES (@idcontato, @idlojasistema) ON CONFLICT DO NOTHING";
                            await using var cmdLigacao = new NpgsqlCommand(sqlLigacao, conn);
                            cmdLigacao.Parameters.AddWithValue("idcontato", contato.Id);
                            cmdLigacao.Parameters.AddWithValue("idlojasistema", int.Parse(idLimpo));
                            await cmdLigacao.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            Console.WriteLine($"Aviso: loja '{idLimpo}' não encontrada (contato {contato.Id}).");
                        }
                    }
                }
            }

            Console.WriteLine($"Página {pagina} de {lastPage} processada.");
            pagina = pagina + 1;
        }

        Console.WriteLine("Concluído!");
    }
}

class RespostaContatos
{
    [JsonPropertyName("data")]
    public Contato[] Data { get; set; }

    [JsonPropertyName("lastPage")]
    public int LastPage { get; set; }
}

class Contato
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("note")]
    public string Note { get; set; }

    [JsonPropertyName("internalName")]
    public string InternalName { get; set; }              // NOVO

    [JsonPropertyName("lastMessageAt")]
    public DateTime? LastMessageAt { get; set; }           // NOVO

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }                // NOVO

    [JsonPropertyName("data")]
    public DadosContato Data { get; set; }
}

class DadosContato
{
    [JsonPropertyName("number")]
    public string Number { get; set; }
}