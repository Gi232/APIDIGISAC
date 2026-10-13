# Integração Digisac + SQL Server

**Integração de contatos entre a API Digisac e SQL Server, desenvolvida em C# e .NET.**

Aplicação de console que consulta os contatos da API Digisac, percorre os resultados paginados e sincroniza os dados com SQL Server. O projeto demonstra autenticação Bearer, desserialização de JSON, consultas parametrizadas e persistência de dados.

Cada pessoa deve configurar a URL da sua instância Digisac, seu próprio token e a conexão com seu banco por variáveis de ambiente. Nenhuma credencial é fornecida no código.

## Tecnologias

- C# e .NET 10
- HttpClient e System.Text.Json
- SQL Server e Microsoft.Data.SqlClient 7.0.2

## Funcionamento

1. Consulta contatos por período e percorre as páginas da API.
2. Insere novos contatos ou atualiza os existentes pelo identificador externo.
3. Interpreta IDs de lojas separados por `|` no campo `note` e registra os vínculos.

O cliente utiliza o contrato da API Digisac. A URL da instância é configurável e a resposta deve conter `data` (lista de contatos) e `lastPage` (última página). Os contatos utilizam os campos `id`, `name`, `note`, `internalName`, `lastMessageAt`, `createdAt` e `data.number`. APIs com outros contratos exigem adaptação dos modelos e filtros.

## Executar

Requisitos: SDK .NET 10, SQL Server e acesso à API de uma instância Digisac.

```powershell
git clone https://github.com/Gi232/APIDIGISAC.git
cd APIDIGISAC
dotnet restore
dotnet build
```

Execute `database/schema.sql` em um banco de desenvolvimento vazio. Configure as variáveis no terminal que executará a aplicação:

| Variável | Finalidade |
|---|---|
| API_CONTACTS_URL | URL HTTPS do endpoint de contatos da sua instância Digisac |
| API_TOKEN | Token Bearer, sem o prefixo Bearer |
| SQL_CONNECTION_STRING | Conexão com seu banco SQL Server |
| SYNC_START_DATE | Início do período, em yyyy-MM-dd |
| SYNC_END_DATE | Fim do período, em yyyy-MM-dd |

Exemplo com valores fictícios:

```powershell
$env:API_CONTACTS_URL = "https://api.example.com/v1/contacts"
$env:SYNC_START_DATE = "2026-01-01"
$env:SYNC_END_DATE = "2026-01-31"
# Configure API_TOKEN e SQL_CONNECTION_STRING no ambiente com seus valores locais.
dotnet run --project API_DIGISAC.csproj
```

Não há token ou conexão padrão. A aplicação interrompe a execução quando falta uma configuração obrigatória. Arquivos `.env` não são carregados automaticamente.

## Estrutura

- `Program.cs`: consulta da API, paginação e sincronização.
- `API_DIGISAC.csproj`: configuração da aplicação .NET.
- `database/schema.sql`: esquema genérico para demonstração.
- `.gitignore`: exclusão de artefatos e arquivos locais de configuração.

## Cuidados com credenciais

Mantenha tokens e conexões fora do código e do controle de versão. Se uma credencial tiver sido publicada anteriormente, revogue ou substitua essa credencial: editar o arquivo não remove versões antigas do histórico.

## Limitações

A integração com o Digisac utiliza filtros `where[lastMessageAt][$gte]` e `where[lastMessageAt][$lte]`; ajuste-os conforme a API. Os vínculos com lojas são adicionados sem remover vínculos anteriores. Não há retry automático nem transação que englobe toda a sincronização. Falhas de SQL nos vínculos são tratadas pelo código existente.

## Autora

[Giovanna Santos — LinkedIn](https://www.linkedin.com/in/giovanna-santos-454a92217/)
