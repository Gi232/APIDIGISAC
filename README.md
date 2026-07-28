# APIDIGISAC

Descrição
---------
APIDIGISAC é uma API desenvolvida em C# (.NET) para fornecer serviços relacionados ao projeto DIGISAC. Este README é um template inicial — ajuste os detalhes (endpoints, variáveis de ambiente, exemplos) conforme a implementação real do seu projeto.

Badges
------
<!-- Substitua pelos badges reais (build, coverage, license, nuget, etc.) -->
![build](https://img.shields.io/badge/build-pendente-lightgrey)
![license](https://img.shields.io/badge/license-MIT-blue)

Tecnologias
----------
- Linguagem: C# (.NET)
- Runtime: .NET 6/7/8 ( ajuste conforme o seu projeto )
- Ferramentas: dotnet CLI, Docker (opcional)

Pré-requisitos
--------------
- .NET SDK (versão compatível com o projeto)
- Git
- (Opcional) Docker e Docker Compose

Instalação (local)
------------------
1. Clone o repositório:
   git clone https://github.com/Gi232/APIDIGISAC.git
2. Entre na pasta do projeto:
   cd APIDIGISAC
3. Restaure dependências e construa:
   dotnet restore
   dotnet build

Executando em desenvolvimento
----------------------------
dotnet run --project src/SeuProjeto.Api
(Altere o caminho acima conforme a estrutura real do seu repositório)

Após iniciar, a API normalmente estará disponível em http://localhost:5000 ou https://localhost:5001 (dependendo da configuração).

Configuração
------------
As configurações ficam tipicamente em `appsettings.json` e `appsettings.Development.json`. Exemplo de variáveis de ambiente (substitua pelos nomes reais do seu projeto):

- ASPNETCORE_ENVIRONMENT=Development
- ConnectionStrings__DefaultConnection=<string de conexão>
- JWT__Issuer=<issuer>
- JWT__Key=<secret>

Exemplo (appsettings.json minimal)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;User Id=...;Password=...;"
  }
}

API — exemplos de uso
---------------------
Ajuste os endpoints conforme a sua implementação. Os exemplos abaixo são genéricos:

- Health check
  GET /health
  curl -v http://localhost:5000/health

- Exemplo de GET
  GET /api/v1/items
  curl -H "Accept: application/json" http://localhost:5000/api/v1/items

- Exemplo de POST
  POST /api/v1/items
  curl -X POST http://localhost:5000/api/v1/items \
    -H "Content-Type: application/json" \
    -d '{"nome":"item exemplo","descricao":"..."}'

Documentação interativa
-----------------------
Se o projeto usar Swagger / OpenAPI, acesse:
http://localhost:5000/swagger

Docker (opcional)
-----------------
Exemplo simples de Dockerfile (ajuste para seu projeto):

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/SeuProjeto.Api/SeuProjeto.Api.csproj"
RUN dotnet publish "src/SeuProjeto.Api/SeuProjeto.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SeuProjeto.Api.dll"]

Estrutura sugerida do repositório
--------------------------------
- src/ — código-fonte da API
- tests/ — projetos de teste
- docs/ — documentação adicional
- .github/workflows/ — workflows de CI

Contato
-------
Criador: Gi232
Email/Contato: gigiovanna.2331@gmail.com

