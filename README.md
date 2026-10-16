# M2S01 — Testes de Segurança · Atividades da Semana 02

Laboratórios práticos de **testes automatizados** e **segurança em aplicações web** com **.NET 10 / C#**.

Cada atividade tem uma pasta própria com um `README.md` passo a passo. Todas rodam **localmente**, sem Docker, Kubernetes, banco de dados instalado ou qualquer serviço externo. A única exceção é a Atividade 05, que usa o GitHub Actions.

> ⚠️ **Aviso:** estes laboratórios simulam vulnerabilidades **de propósito**, apenas para estudo.
> Execute somente na sua máquina e **nunca** teste ataques contra sistemas de terceiros.

---

## Atividades

| # | Atividade | O que você vai aprender | Tipo |
|---|---|---|---|
| 01 | [TDD e Cobertura de Testes](atividade-01-coverage-test/README.md) | Escrever o teste antes do código (xUnit) e gerar relatório de cobertura (coverlet + ReportGenerator) | Você cria do zero |
| 02 | [SQL Injection](atividade-02-sql-injection/README.md) | Explorar uma API vulnerável com `' OR 1=1 --` e prevenir com consultas parametrizadas | Você cria do zero |
| 03 | [XSS (Cross-Site Scripting)](atividade-03-xss/README.md) | Reproduzir um *Stored XSS* em Razor Pages e corrigir com HTML encoding | Você cria do zero |
| 04 | [CSRF (Cross-Site Request Forgery)](atividade-04-csrf/README.md) | Forjar uma requisição a partir de outro site, entender `SameSite` e corrigir com token anti-forgery | Código **já pronto** na pasta |
| 05 | [Nosso primeiro CI](atividade-05-ci/README.md) | Criar uma pipeline no GitHub Actions com build, testes, cobertura e SAST (CodeQL) | Usa o projeto da Atividade 01 |

### Ordem recomendada

Faça as atividades **na ordem numérica**.

- A **Atividade 05** depende do projeto `LabDivisao` criado na **Atividade 01**. Guarde-o.
- As atividades 02, 03 e 04 são independentes entre si.

### Relação com o OWASP Top 10

| Atividade | Categoria OWASP Top 10 |
|---|---|
| 02 — SQL Injection | **A03 – Injection** |
| 03 — XSS | **A03 – Injection** (injeção de script no navegador) |
| 04 — CSRF | **A01 – Broken Access Control** |
| 02 — pacote `Microsoft.Data.Sqlite` | **A06 – Vulnerable and Outdated Components** (veja o aviso sobre a versão `10.0.0`) |
| 05 — SAST no CI | Prática de prevenção que ajuda a detectar várias categorias acima automaticamente |

---

## Pré-requisitos

| Ferramenta | Obrigatória para | Como verificar | Onde baixar |
|---|---|---|---|
| **.NET 10 SDK** | Todas | `dotnet --list-sdks` (deve aparecer `10.x`) | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| **Git** | Clonar o repositório e Atividade 05 | `git --version` | <https://git-scm.com/downloads> |
| **Navegador** (Chrome, Edge ou Firefox) | Atividades 03 e 04 | — | — |
| **curl** | Atividade 02 (opcional, dá para usar o navegador) | `curl --version` (no Windows: `curl.exe --version`) | Já vem no Windows 10/11, macOS e na maioria das distros Linux |
| **Conta no GitHub** | Atividade 05 | — | <https://github.com/signup> |
| Editor de código (VS Code, Rider ou Visual Studio) | Recomendado | — | <https://code.visualstudio.com> |

Os comandos funcionam em **Windows, Linux e macOS**. Quando existe diferença entre os sistemas, o README da atividade mostra os comandos de cada um.

---

## Como começar

```bash
git clone https://github.com/Desenvolvedor-BackEnd-NET-T1/M2S01-TestesDeSeguranca-Atividade-Semana02.git
cd M2S01-TestesDeSeguranca-Atividade-Semana02
dotnet --list-sdks
```

Depois abra o `README.md` da **Atividade 01** e siga os passos.

> 💡 **Dica:** nas atividades 01, 02 e 03 você cria o projeto do zero. Faça isso **fora** desta pasta (por exemplo, em `~/labs` ou `C:\labs`) para não misturar o seu código com o repositório do curso.

---

## Estrutura do repositório

```text
.
├── README.md                          ← você está aqui
├── atividade-01-coverage-test/
│   └── README.md
├── atividade-02-sql-injection/
│   └── README.md
├── atividade-03-xss/
│   └── README.md
├── atividade-04-csrf/
│   ├── README.md
│   ├── LabCsrf/                       ← site vítima (ASP.NET Core MVC), intencionalmente vulnerável
│   └── site-malicioso/ataque.html     ← página do atacante
└── atividade-05-ci/
    └── README.md
```

---

## Portas usadas nos laboratórios

Cada atividade usa uma porta fixa para os endereços dos READMEs funcionarem igual para todos.

| Atividade | Endereço |
|---|---|
| 02 — SQL Injection | `http://localhost:5080` |
| 03 — XSS | `http://localhost:5090` |
| 04 — CSRF | `https://localhost:7001` |

> Evitamos a porta **5000** porque no macOS ela é usada pelo **AirPlay Receiver** e responde `403` no lugar da sua aplicação.

---

## Dicas gerais

- **Windows:** use o **PowerShell**. Para chamar APIs, use `curl.exe` (no PowerShell, `curl` é um apelido de outro comando).
- **macOS / Linux (zsh):** coloque URLs com `?` entre **aspas duplas**, senão aparece `zsh: no matches found`.
- **Parar um servidor:** pressione `Ctrl + C` no terminal onde ele está rodando.
- **Alterou o código?** Pare o servidor e rode `dotnet run` de novo. Os dados em memória são apagados a cada reinício.
- **Idioma do SDK:** as mensagens podem aparecer em português (`Aprovado!`) ou em inglês (`Passed!`), conforme o idioma do sistema. As duas estão corretas.
- **Leia o "porquê":** cada README explica o motivo de cada passo. Entender a causa da vulnerabilidade é mais importante do que fazer o ataque funcionar.

---

## Problemas comuns (em qualquer atividade)

| Problema | Solução |
|---|---|
| `dotnet: command not found` | Instale o .NET 10 SDK e **abra um terminal novo**. |
| `dotnet --list-sdks` não mostra `10.x` | Você tem só uma versão antiga. Instale o .NET 10 SDK (as versões podem conviver). |
| `address already in use` | A porta está ocupada por outro programa ou por uma execução anterior. Feche o outro terminal ou troque a porta, conforme o README da atividade. |
| `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0` | O SDK ativo é antigo. Confira se existe um `global.json` em alguma pasta acima fixando outra versão. |
| Erros de restauração de pacotes (`NU1301`) | Verifique sua conexão com a internet: na primeira execução o `dotnet` baixa os pacotes do NuGet. |

Ficou travado? Releia o passo anterior com atenção. Na maioria das vezes falta um arquivo, uma pasta errada (`cd`) ou um servidor que não foi reiniciado.
