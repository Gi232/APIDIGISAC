using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

class Program
{
    static async Task Main(string[] args)
    {
        string url = ObterConfiguracao("API_CONTACTS_URL");
        string token = ObterConfiguracao("API_TOKEN");
        string connString = ObterConfiguracao("SQL_CONNECTION_STRING");
        string dataInicial = ObterConfiguracao("SYNC_START_DATE");
        string dataFinal = ObterConfiguracao("SYNC_END_DATE");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var apiUri) || apiUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("API_CONTACTS_URL deve ser uma URL HTTPS.");
        if (!DateOnly.TryParseExact(dataInicial, "yyyy-MM-dd", out var inicio) ||
            !DateOnly.TryParseExact(dataFinal, "yyyy-MM-dd", out var fim) || inicio > fim)
            throw new InvalidOperationException("Informe datas válidas no formato yyyy-MM-dd, com início anterior ou igual ao fim.");

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await using var conn = new SqlConnection(connString);
        await conn.OpenAsync();

        int pagina = 1;
        int lastPage = 1;

        while (pagina <= lastPage)
        {
            RespostaContatos dados = await BuscarPaginaContatos(client, url, pagina, dataInicial, dataFinal);
            lastPage = dados.LastPage;

            foreach (Contato contato in dados.Data!)
            {
                await InserirOuAtualizarContato(contato, conn);
                await ProcessarLigacoesLoja(contato, conn);
            }

            Console.WriteLine($"Página {pagina} de {lastPage} processada.");
            pagina = pagina + 1;
        }

        Console.WriteLine("Concluído!");
    }

    static string ObterConfiguracao(string nome)
    {
        var valor = Environment.GetEnvironmentVariable(nome);
        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException($"Configure a variável de ambiente {nome}.");
        return valor;
    }

    static async Task<RespostaContatos> BuscarPaginaContatos(HttpClient client, string url, int pagina, string dataInicial, string dataFinal)
    {
        string urlComPagina = $"{url}?page={pagina}&where[lastMessageAt][$gte]={dataInicial}&where[lastMessageAt][$lte]={dataFinal}";
        using HttpResponseMessage resposta = await client.GetAsync(urlComPagina);
        resposta.EnsureSuccessStatusCode();
        string conteudoJson = await resposta.Content.ReadAsStringAsync();
        var dados = JsonSerializer.Deserialize<RespostaContatos>(conteudoJson)
            ?? throw new InvalidOperationException("A API retornou uma resposta JSON vazia.");
        if (dados.Data is null || dados.LastPage < 1)
            throw new InvalidOperationException("A resposta deve conter data e lastPage válidos.");
        return dados;
    }

    static async Task InserirOuAtualizarContato(Contato contato, SqlConnection conn)
    {
        if (string.IsNullOrWhiteSpace(contato.Id))
            throw new InvalidOperationException("A API retornou um contato sem identificador.");

        string sql = @"MERGE INTO Contatos AS alvo
                       USING (SELECT @idContatoExterno AS idContatoExterno, @nome AS nome, @numero AS numero, 
                                     @note AS note, @internalname AS internalname, 
                                     @lastmessageat AS lastmessageat, @createdat AS createdat) AS origem
                       ON alvo.idContatoExterno = origem.idContatoExterno
                       WHEN MATCHED THEN
                           UPDATE SET
                               nome = origem.nome,
                               numero = origem.numero,
                               note = origem.note,
                               internalname = origem.internalname,
                               lastmessageat = origem.lastmessageat,
                               createdat = origem.createdat
                       WHEN NOT MATCHED THEN
                           INSERT (idContatoExterno, nome, numero, note, internalname, lastmessageat, createdat)
                           VALUES (origem.idContatoExterno, origem.nome, origem.numero, origem.note, 
                                   origem.internalname, origem.lastmessageat, origem.createdat);";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idContatoExterno", contato.Id);
        cmd.Parameters.AddWithValue("nome", (object)contato.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("numero", (object)contato.Data?.Number ?? DBNull.Value);
        cmd.Parameters.AddWithValue("note", (object)contato.Note ?? DBNull.Value);
        cmd.Parameters.AddWithValue("internalname", (object)contato.InternalName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("lastmessageat", (object)contato.LastMessageAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("createdat", (object)contato.CreatedAt ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    static async Task ProcessarLigacoesLoja(Contato contato, SqlConnection conn)
    {
        if (string.IsNullOrEmpty(contato.Note)) return;

        string[] idsLoja = contato.Note.Split('|');

        foreach (string pedaco in idsLoja)
        {
            string idLimpo = pedaco.Trim();

            if (!int.TryParse(idLimpo, out int idLojaConvertido))
            {
                Console.WriteLine($"Aviso: valor '{idLimpo}' não é um ID de loja válido (contato {contato.Id}).");
                continue;
            }

            try
            {
                string sqlLigacao = @"IF NOT EXISTS (
                                          SELECT 1 FROM ContatoLojas 
                                          WHERE idContatoExterno = @idContatoExterno AND idloja = @idloja
                                       )
                                       BEGIN
                                           INSERT INTO ContatoLojas (idContatoExterno, idloja) 
                                           VALUES (@idContatoExterno, @idloja)
                                       END";

                await using var cmdLigacao = new SqlCommand(sqlLigacao, conn);
                cmdLigacao.Parameters.AddWithValue("idContatoExterno", contato.Id);
                cmdLigacao.Parameters.AddWithValue("idloja", idLojaConvertido);
                await cmdLigacao.ExecuteNonQueryAsync();
            }
            catch (SqlException)
            {
                Console.WriteLine($"Aviso: loja '{idLimpo}' não encontrada (contato {contato.Id}).");
            }
        }
    }
}

class RespostaContatos
{
    [JsonPropertyName("data")]
    public Contato[]? Data { get; set; }

    [JsonPropertyName("lastPage")]
    public int LastPage { get; set; }
}

class Contato
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("internalName")]
    public string? InternalName { get; set; }

    [JsonPropertyName("lastMessageAt")]
    public DateTime? LastMessageAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("data")]
    public DadosContato? Data { get; set; }
}

class DadosContato
{
    [JsonPropertyName("number")]
    public string? Number { get; set; }
}