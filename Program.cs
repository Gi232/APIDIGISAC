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
        string url = "https://controlxrm.digisac.me/api/v1/contacts";
        string token = "60b7fa3f351bca976997c61a09aac92c7c838bc5";
        string connString = "Server=18.229.25.35;Database=xrm_producao;UID=XRMusuario;PWD=xrm;Connection Timeout=600;TrustServerCertificate=true";
        string dataInicial = "2026-07-20";
        string dataFinal = "2026-07-27";

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

            foreach (Contato contato in dados.Data)
            {
                await InserirOuAtualizarContato(contato, conn);
                await ProcessarLigacoesLoja(contato, conn);
            }

            Console.WriteLine($"Página {pagina} de {lastPage} processada.");
            pagina = pagina + 1;
        }

        Console.WriteLine("Concluído!");
    }

    static async Task<RespostaContatos> BuscarPaginaContatos(HttpClient client, string url, int pagina, string dataInicial, string dataFinal)
    {
        string urlComPagina = $"{url}?page={pagina}&where[lastMessageAt][$gte]={dataInicial}&where[lastMessageAt][$lte]={dataFinal}";
        HttpResponseMessage resposta = await client.GetAsync(urlComPagina);
        string conteudoJson = await resposta.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RespostaContatos>(conteudoJson);
    }

    static async Task InserirOuAtualizarContato(Contato contato, SqlConnection conn)
    {
        string sql = @"MERGE INTO Contatos_Digisac AS alvo
                       USING (SELECT @idContatoDigisac AS idContatoDigisac, @nome AS nome, @numero AS numero, 
                                     @note AS note, @internalname AS internalname, 
                                     @lastmessageat AS lastmessageat, @createdat AS createdat) AS origem
                       ON alvo.idContatoDigisac = origem.idContatoDigisac
                       WHEN MATCHED THEN
                           UPDATE SET
                               nome = origem.nome,
                               numero = origem.numero,
                               note = origem.note,
                               internalname = origem.internalname,
                               lastmessageat = origem.lastmessageat,
                               createdat = origem.createdat
                       WHEN NOT MATCHED THEN
                           INSERT (idContatoDigisac, nome, numero, note, internalname, lastmessageat, createdat)
                           VALUES (origem.idContatoDigisac, origem.nome, origem.numero, origem.note, 
                                   origem.internalname, origem.lastmessageat, origem.createdat);";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("idContatoDigisac", contato.Id);
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
                                          SELECT 1 FROM Contatos_Digisac_loja 
                                          WHERE idContatoDigisac = @idContatoDigisac AND idloja = @idloja
                                       )
                                       BEGIN
                                           INSERT INTO Contatos_Digisac_loja (idContatoDigisac, idloja) 
                                           VALUES (@idContatoDigisac, @idloja)
                                       END";

                await using var cmdLigacao = new SqlCommand(sqlLigacao, conn);
                cmdLigacao.Parameters.AddWithValue("idContatoDigisac", contato.Id);
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
    public Contato[] Data { get; set; }

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