# Atividade 02 — SQL Injection

## Objetivo

Simular um ataque de **SQL Injection** em uma Web API e aprender como **preveni-lo**.

Você vai criar uma API com dois endpoints que fazem a mesma busca de e-mails:

| Endpoint | Como monta o SQL | Resultado com o ataque |
|---|---|---|
| `/api/usuarios/vulneravel` | Concatenando texto (`"..." + email + "..."`) | ❌ Vaza **todos** os e-mails |
| `/api/usuarios/seguro` | Usando **parâmetros** (`@email`) | ✅ Não retorna nada |

> ⚠️ Laboratório **local**, apenas para estudo. Nunca teste ataques em sistemas de terceiros.

## O que você vai usar

| Ferramenta | Para quê |
|---|---|
| .NET 10 SDK | Criar e executar a Web API |
| `Microsoft.Data.Sqlite` | Banco SQLite **em memória** (sem instalar nada além do pacote) |
| `curl` ou navegador | Chamar os endpoints |

> **Por que `Microsoft.Data.Sqlite` e não EF Core?** Para mostrar a vulnerabilidade precisamos escrever o SQL "na mão". O `Microsoft.Data.Sqlite` é o driver ADO.NET que o próprio EF Core usa por baixo, mais leve e direto para este lab.

> Funciona em **Windows, Linux e macOS**. No Windows use o **PowerShell** e o comando `curl.exe` (já vem instalado no Windows 10/11).

---

## Passo 0 — Verificar o ambiente

```bash
dotnet --list-sdks
```

Deve aparecer uma linha começando com `10.`.

---

## Passo 1 — Criar o projeto Web API

```bash
dotnet new webapi -n LabSqlInjection -f net10.0 --use-controllers --use-program-main
cd LabSqlInjection
```

- `--use-controllers`: cria a API com **Controllers** (em vez de *minimal APIs*).
- `--use-program-main`: gera o `Program.cs` no estilo clássico (*legacy style*), com `class Program` e `static void Main`.

Remova os arquivos de exemplo do template:

| Sistema | Comando |
|---|---|
| Windows (PowerShell) | `Remove-Item WeatherForecast.cs, Controllers/WeatherForecastController.cs, LabSqlInjection.http` |
| macOS / Linux | `rm WeatherForecast.cs Controllers/WeatherForecastController.cs LabSqlInjection.http` |

---

## Passo 2 — Adicionar o pacote do SQLite

```bash
dotnet add package Microsoft.Data.Sqlite --version 10.0.13
```

> ⚠️ **Não use a versão `10.0.0`.** Ela depende de uma biblioteca nativa do SQLite com vulnerabilidade conhecida (o build mostra o aviso `NU1903`). Em um curso de segurança, essa também é uma lição: **dependências também precisam ser atualizadas.**

---

## Passo 3 — Criar e popular o banco em memória

Substitua todo o conteúdo de `Program.cs` por:

```csharp
using Microsoft.Data.Sqlite;

namespace LabSqlInjection;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Banco SQLite em memória: ele existe apenas enquanto esta conexão estiver aberta.
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        CriarEPopularBanco(conexao);

        // A mesma conexão é compartilhada com os controllers (apenas para fins de laboratório).
        builder.Services.AddSingleton(conexao);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    private static void CriarEPopularBanco(SqliteConnection conexao)
    {
        var comando = conexao.CreateCommand();
        comando.CommandText = @"
            CREATE TABLE Usuarios (
                Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome  TEXT NOT NULL,
                Email TEXT NOT NULL
            );

            INSERT INTO Usuarios (Nome, Email) VALUES
                ('Ana Souza',      'ana.souza@exemplo.com'),
                ('Bruno Lima',     'bruno.lima@exemplo.com'),
                ('Carla Mendes',   'carla.mendes@exemplo.com'),
                ('Diego Rocha',    'diego.rocha@exemplo.com'),
                ('Elisa Martins',  'elisa.martins@exemplo.com');";
        comando.ExecuteNonQuery();
    }
}
```

**Por que assim?**

- `Data Source=:memory:` cria o banco **na memória RAM**. Não há arquivo nem servidor de banco.
- O banco em memória **some quando a conexão fecha**. Por isso abrimos uma única conexão e a registramos como `Singleton`: ela fica viva enquanto a API estiver rodando.
- A cada vez que a API inicia, a tabela é recriada com os mesmos **5 e-mails**.
- Removemos o `app.UseHttpsRedirection()` do template porque o lab roda apenas em HTTP local.

---

## Passo 4 — Criar o Controller com os dois endpoints

Crie o arquivo `Controllers/UsuariosController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace LabSqlInjection.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly SqliteConnection _conexao;

    public UsuariosController(SqliteConnection conexao)
    {
        _conexao = conexao;
    }

    // VULNERÁVEL: o valor digitado pelo usuário é concatenado direto no SQL.
    [HttpGet("vulneravel")]
    public IActionResult BuscarVulneravel([FromQuery] string email)
    {
        var comando = _conexao.CreateCommand();
        comando.CommandText = "SELECT Email FROM Usuarios WHERE Email = '" + email + "'";

        return Ok(new
        {
            sqlExecutado = comando.CommandText,
            emails = LerEmails(comando)
        });
    }

    // SEGURO: o valor é enviado como parâmetro, separado do comando SQL.
    [HttpGet("seguro")]
    public IActionResult BuscarSeguro([FromQuery] string email)
    {
        var comando = _conexao.CreateCommand();
        comando.CommandText = "SELECT Email FROM Usuarios WHERE Email = @email";
        comando.Parameters.AddWithValue("@email", email);

        return Ok(new
        {
            sqlExecutado = comando.CommandText,
            emails = LerEmails(comando)
        });
    }

    private static List<string> LerEmails(SqliteCommand comando)
    {
        var emails = new List<string>();

        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            emails.Add(leitor.GetString(0));
        }

        return emails;
    }
}
```

> A resposta devolve também o campo `sqlExecutado`, só para fins didáticos: assim você **vê** o SQL que chegou ao banco. Nunca faça isso em produção.

---

## Passo 5 — Executar a API

```bash
dotnet run --urls http://localhost:5080
```

Aguarde a mensagem:

```text
Now listening on: http://localhost:5080
```

Deixe esse terminal aberto e **abra um segundo terminal** para os próximos passos.

---

## Passo 6 — Testar com um e-mail válido (os dois devem funcionar)

**Endpoint vulnerável:**

```bash
curl "http://localhost:5080/api/usuarios/vulneravel?email=ana.souza@exemplo.com"
```

**Endpoint seguro:**

```bash
curl "http://localhost:5080/api/usuarios/seguro?email=ana.souza@exemplo.com"
```

> **Windows (PowerShell):** troque `curl` por `curl.exe`.
> Você também pode colar as URLs direto no **navegador**.

**Resultado esperado (os dois retornam só 1 e-mail):**

```json
{"sqlExecutado":"SELECT Email FROM Usuarios WHERE Email = 'ana.souza@exemplo.com'","emails":["ana.souza@exemplo.com"]}
```

```json
{"sqlExecutado":"SELECT Email FROM Usuarios WHERE Email = @email","emails":["ana.souza@exemplo.com"]}
```

✅ Com uma entrada "normal", os dois endpoints se comportam igual.

---

## Passo 7 — Atacar com SQL Injection

Agora, em vez de um e-mail, vamos enviar o texto:

```text
' OR 1=1 --
```

Na URL ele precisa ser **codificado** (espaço vira `%20`, `'` vira `%27`, `=` vira `%3D`): `%27%20OR%201%3D1%20--`

**Endpoint vulnerável:**

```bash
curl "http://localhost:5080/api/usuarios/vulneravel?email=%27%20OR%201%3D1%20--"
```

**Resultado: ❌ vazou TODOS os e-mails**

```json
{
  "sqlExecutado": "SELECT Email FROM Usuarios WHERE Email = '' OR 1=1 --'",
  "emails": [
    "ana.souza@exemplo.com",
    "bruno.lima@exemplo.com",
    "carla.mendes@exemplo.com",
    "diego.rocha@exemplo.com",
    "elisa.martins@exemplo.com"
  ]
}
```

**Endpoint seguro:**

```bash
curl "http://localhost:5080/api/usuarios/seguro?email=%27%20OR%201%3D1%20--"
```

**Resultado: ✅ nenhum e-mail**

```json
{"sqlExecutado":"SELECT Email FROM Usuarios WHERE Email = @email","emails":[]}
```

> 💡 No navegador você pode digitar sem codificar: `http://localhost:5080/api/usuarios/vulneravel?email=' OR 1=1 --` (o navegador codifica sozinho).

---

## Passo 8 — Entendendo o que aconteceu

### No endpoint vulnerável

O texto do usuário foi **colado dentro** do comando SQL:

```text
SELECT Email FROM Usuarios WHERE Email = '' OR 1=1 --'
                                         └┬┘ └──┬──┘ └┬┘
          o ' do atacante fecha a string ─┘     │     │
          condição SEMPRE verdadeira ───────────┘     │
          -- comenta o resto (o ' que sobrou) ────────┘
```

O banco não consegue diferenciar o que é **comando** do que é **dado**. O atacante mudou a lógica da consulta: `Email = ''` **OU** `1=1` (sempre verdadeiro) → retorna todas as linhas.

### No endpoint seguro

O comando SQL é enviado **separado** do valor:

```text
Comando:   SELECT Email FROM Usuarios WHERE Email = @email
Parâmetro: @email = "' OR 1=1 --"
```

O banco trata `' OR 1=1 --` como um **texto qualquer**. Ele procura um e-mail que seja literalmente igual a `' OR 1=1 --`. Como não existe, retorna lista vazia. O texto **nunca vira código SQL**.

### Como prevenir

| ✅ Faça | ❌ Não faça |
|---|---|
| Use **parâmetros** (`@email`, `Parameters.AddWithValue`) | Concatenar texto do usuário no SQL (`+`, `string.Format`, `$"..."`) |
| Use ORMs (EF Core, Dapper) com parâmetros | `FromSqlRaw($"... {valor}")` com interpolação |
| Valide formato da entrada (ex: é um e-mail?) como camada **extra** | Confiar apenas em "limpar" aspas manualmente |
| Use um usuário de banco com o **mínimo de permissões** | Conectar no banco como administrador |

---

## Passo 9 — Encerrar

No terminal da API pressione `Ctrl + C`. Como o banco é em memória, tudo é apagado.

## Problemas comuns

| Problema | Solução |
|---|---|
| `zsh: no matches found` (macOS/Linux) | Coloque a URL entre **aspas duplas**, como nos exemplos. |
| PowerShell retorna um objeto estranho em vez do JSON | Use `curl.exe` em vez de `curl` (no PowerShell `curl` é um apelido de `Invoke-WebRequest`). |
| `Failed to bind to address ... address already in use` | A porta 5080 está ocupada. Use outra: `dotnet run --urls http://localhost:5050` e ajuste as URLs. |
| `HTTP 400` com a mensagem `The email field is required` | Faltou o `?email=...` na URL. |
| Aviso `NU1903` no build | Você instalou a versão `10.0.0` do pacote. Rode o comando do Passo 2 com `--version 10.0.13`. |
