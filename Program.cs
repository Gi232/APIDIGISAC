using System;
using System.Net.Http; // import para HttpClient (requisições HTTP)
using System.Net.Http.Headers;
using System.Text.Json;     // import para JsonSerializer (conversão de JSON para objetos C#)
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Npgsql; // import para Npgsql (conexão com PostgreSQL)

class Program
{
    static async Task Main(string[] args)
    {
        string url = "https://controlxrm.digisac.me/api/v1/contacts";
        string token = "60b7fa3f351bca976997c61a09aac92c7c838bc5";
        string connString = "Host=ep-lucky-sun-ayo24u1p-pooler.c-5.us-east-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_aSOJP3H4CYum;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        string dataInicial = "2026-07-01"; // Data inicial para filtrar os contatos
        string dataFinal = "2026-07-31"; // Data final para filtrar os contatos

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =    
            new AuthenticationHeaderValue("Bearer", token); // Adiciona o token de autenticação no cabeçalho da requisição

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync(); // Abre a conexão com o banco de dados

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

                string sql = "INSERT INTO contato (id, nome, numero, note) VALUES (@id, @nome, @numero, @note) ON CONFLICT (id) DO NOTHING"; 

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", contato.Id);
                cmd.Parameters.AddWithValue("nome", (object)contato.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("numero", (object)contato.Data?.Number ?? DBNull.Value);
                cmd.Parameters.AddWithValue("note", (object)contato.Note ?? DBNull.Value); 

                await cmd.ExecuteNonQueryAsync();
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

    [JsonPropertyName("data")]
    public DadosContato Data { get; set; }
}

class DadosContato
{
    [JsonPropertyName("number")]
    public string Number { get; set; }
}