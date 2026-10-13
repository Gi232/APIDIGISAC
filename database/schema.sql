-- Esquema de demonstração para um banco de desenvolvimento vazio.
CREATE TABLE dbo.Contatos (
    idContatoExterno nvarchar(128) NOT NULL PRIMARY KEY,
    nome nvarchar(255) NULL,
    numero nvarchar(100) NULL,
    note nvarchar(max) NULL,
    internalname nvarchar(255) NULL,
    lastmessageat datetime2 NULL,
    createdat datetime2 NULL
);
CREATE TABLE dbo.ContatoLojas (
    idContatoExterno nvarchar(128) NOT NULL,
    idloja int NOT NULL,
    CONSTRAINT PK_ContatoLojas PRIMARY KEY (idContatoExterno, idloja),
    CONSTRAINT FK_ContatoLojas_Contatos FOREIGN KEY (idContatoExterno)
        REFERENCES dbo.Contatos(idContatoExterno)
);
