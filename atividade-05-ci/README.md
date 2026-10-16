# Atividade 05 — Nosso primeiro CI

## Objetivo

Criar uma **pipeline de CI** (Integração Contínua) com **GitHub Actions** que, a cada `push`:

1. Faz o **build** do projeto;
2. Executa os **testes**;
3. Gera o **relatório de cobertura** de testes;
4. Gera o **relatório SAST** (análise estática de segurança).

Depois você vai fazer o `push` e **acompanhar** a pipeline rodando no GitHub.

## O que você vai usar

| Ferramenta | Para quê |
|---|---|
| Projeto `LabDivisao` da **Atividade 01** | Código que a pipeline vai compilar e testar |
| Git | Versionar e enviar o código |
| Conta no GitHub | Hospedar o repositório e rodar a pipeline |
| GitHub Actions | Executar a pipeline (já vem com o GitHub) |
| ReportGenerator | Relatório de cobertura (já configurado na Atividade 01) |
| CodeQL | Ferramenta de SAST do GitHub |

> Funciona em **Windows, Linux e macOS**. No Windows use o **PowerShell**.
> A pipeline em si roda nos servidores do GitHub (Linux), não na sua máquina.

---

## Passo 0 — Pré-requisitos

```bash
dotnet --list-sdks
git --version
```

- Deve aparecer o .NET `10.x` e uma versão do Git.
- Você precisa do projeto **`LabDivisao`** da Atividade 01 funcionando, **incluindo** o arquivo `dotnet-tools.json` (criado no Passo 4.2 da Atividade 01).

Ainda não tem o Git? Instale em <https://git-scm.com/downloads>.

---

## Passo 1 — Criar o repositório no GitHub

1. Acesse <https://github.com/new>.
2. **Repository name:** `meu-primeiro-ci`
3. Marque **Public**.
4. **Não** marque "Add a README file", nem `.gitignore`, nem licença (o repositório deve nascer **vazio**).
5. Clique em **Create repository** e deixe a página aberta. Você vai precisar da URL dele.

> **Por que público?** O CodeQL (SAST) é gratuito em repositórios **públicos**. Em repositórios privados ele exige a licença *GitHub Advanced Security*. Veja a seção [Problemas comuns](#problemas-comuns) se precisar usar um privado.

---

## Passo 2 — Preparar a pasta do repositório

Crie uma pasta nova e copie para ela **o conteúdo** da pasta `LabDivisao` da Atividade 01.

**Windows (PowerShell):**

```powershell
mkdir meu-primeiro-ci
Copy-Item -Recurse <caminho-da-atividade-01>\LabDivisao\* meu-primeiro-ci\
cd meu-primeiro-ci
```

**macOS / Linux:**

```bash
mkdir meu-primeiro-ci
cp -R <caminho-da-atividade-01>/LabDivisao/. meu-primeiro-ci/
cd meu-primeiro-ci
```

> Troque `<caminho-da-atividade-01>` pelo caminho real na sua máquina.

Crie um `.gitignore` para **não** enviar arquivos gerados (pastas `bin`, `obj`, `TestResults`...):

```bash
dotnet new gitignore
```

Abra o `.gitignore` em um editor e adicione **no final** a linha:

```text
coveragereport/
```

A pasta deve ficar assim (as pastas `bin`, `obj`, `TestResults` e `coveragereport` podem existir, o Git vai ignorá-las):

```text
meu-primeiro-ci/
├── .gitignore
├── dotnet-tools.json        ← obrigatório (ReportGenerator)
├── LabDivisao.slnx
├── LabDivisao.App/
│   ├── Calculadora.cs
│   ├── LabDivisao.App.csproj
│   └── Program.cs
└── LabDivisao.Tests/
    ├── CalculadoraTests.cs
    └── LabDivisao.Tests.csproj
```

---

## Passo 3 — Rodar a pipeline "na mão" antes de automatizar

Uma boa prática: antes de criar o CI, rode **os mesmos comandos** que ele vai rodar. Se funcionar na sua máquina, deve funcionar no CI.

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool restore
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;MarkdownSummaryGithub"
```

**Resultado esperado:**

- O teste aparece como `Aprovado` (`Passed`);
- A pasta `coveragereport` é criada com os arquivos `index.html` e `SummaryGithub.md`.

---

## Passo 4 — Criar o arquivo da pipeline

O GitHub Actions procura pipelines na pasta **`.github/workflows`** na raiz do repositório.

Crie as pastas:

```bash
mkdir .github
mkdir .github/workflows
```

Crie o arquivo **`.github/workflows/ci.yml`** com o conteúdo:

```yaml
name: CI

# Quando a pipeline roda
on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:

permissions:
  contents: read

jobs:
  # ---------------------------------------------------------------
  # JOB 1: build + testes + cobertura
  # ---------------------------------------------------------------
  build-test-coverage:
    name: Build, testes e cobertura
    runs-on: ubuntu-latest

    steps:
      - name: Baixar o código
        uses: actions/checkout@v7

      - name: Instalar o .NET 10
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '10.0.x'

      - name: Restaurar dependências
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Testes + coleta de cobertura
        run: dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory ./TestResults

      - name: Restaurar ferramentas (ReportGenerator)
        run: dotnet tool restore

      - name: Gerar relatório de cobertura
        run: dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;MarkdownSummaryGithub"

      - name: Publicar resumo da cobertura na execução
        run: cat coveragereport/SummaryGithub.md >> "$GITHUB_STEP_SUMMARY"

      - name: Salvar relatório de cobertura (HTML)
        uses: actions/upload-artifact@v7
        with:
          name: relatorio-cobertura
          path: coveragereport

  # ---------------------------------------------------------------
  # JOB 2: SAST (análise estática de segurança) com CodeQL
  # ---------------------------------------------------------------
  sast:
    name: SAST (CodeQL)
    runs-on: ubuntu-latest

    permissions:
      contents: read
      actions: read
      security-events: write

    steps:
      - name: Baixar o código
        uses: actions/checkout@v7

      - name: Iniciar o CodeQL
        uses: github/codeql-action/init@v4
        with:
          languages: csharp
          build-mode: none

      - name: Analisar o código
        uses: github/codeql-action/analyze@v4
        with:
          category: "/language:csharp"
          output: sarif-results

      - name: Salvar relatório SAST (SARIF)
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: relatorio-sast
          path: sarif-results
```

> ⚠️ Em YAML a **indentação importa**. Use **espaços**, nunca TAB.

### Entendendo o arquivo

| Trecho | O que faz |
|---|---|
| `on: push / pull_request` | Roda a pipeline a cada `push` ou *pull request* na branch `main` |
| `on: workflow_dispatch` | Cria um botão **Run workflow** para rodar manualmente |
| `permissions: contents: read` | Por segurança, a pipeline começa só com permissão de **leitura** |
| `jobs` | Os dois *jobs* (`build-test-coverage` e `sast`) rodam **em paralelo**, cada um em uma máquina Linux nova |
| `actions/checkout` | Baixa o código do repositório para a máquina da pipeline |
| `actions/setup-dotnet` | Instala o .NET 10 |
| `dotnet restore / build / test` | Os mesmos comandos do Passo 3 |
| `dotnet tool restore` | Instala o ReportGenerator a partir do `dotnet-tools.json` |
| `$GITHUB_STEP_SUMMARY` | Mostra o resumo da cobertura na própria página da execução |
| `actions/upload-artifact` | Salva os relatórios para você **baixar** depois |
| `codeql-action/init` + `analyze` | Analisa o código C# procurando vulnerabilidades (SAST). `build-mode: none` analisa sem precisar compilar |
| `security-events: write` | Permite ao CodeQL publicar os alertas na aba **Security** do repositório |

---

## Passo 5 — Fazer o commit e o push

Ainda dentro da pasta `meu-primeiro-ci`:

```bash
git init
git add .
git commit -m "Primeiro CI: build, testes, cobertura e SAST"
git branch -M main
git remote add origin https://github.com/<seu-usuario>/meu-primeiro-ci.git
git push -u origin main
```

> Troque `<seu-usuario>` pelo seu usuário do GitHub (ou copie a URL da página do Passo 1).

Antes do `commit`, confira com `git status` que **não** aparecem as pastas `bin`, `obj`, `TestResults` ou `coveragereport`.

> Se o `git commit` reclamar de nome/e-mail, configure uma única vez:
> `git config --global user.name "Seu Nome"` e `git config --global user.email "seu@email.com"`.
>
> Se o `git push` pedir senha, o GitHub **não aceita** a senha da conta. Use o login pelo navegador que o Git abre, ou um *Personal Access Token*.

---

## Passo 6 — Acompanhar a pipeline

1. No GitHub, abra o repositório e clique na aba **Actions**.
2. Clique na execução **"Primeiro CI: build, testes, cobertura e SAST"**.
3. Você verá os dois jobs: **Build, testes e cobertura** e **SAST (CodeQL)**.
   - 🟡 amarelo = rodando
   - ✅ verde = sucesso
   - ❌ vermelho = falhou
4. Clique em um job para ver o **log de cada step**. Abra o step **Testes + coleta de cobertura** e procure o resultado do teste.

A execução leva de 2 a 5 minutos.

### Ver o relatório de cobertura

- **Resumo:** na página da execução (aba **Summary**), role para baixo. A tabela de cobertura aparece ali, por classe:

  ```text
  LabDivisao.App.Calculadora   100%
  LabDivisao.App.Program         0%
  ```

- **Relatório completo:** no final da página, em **Artifacts**, baixe **`relatorio-cobertura`**. Descompacte e abra o `index.html` no navegador.

### Ver o relatório SAST

- Aba **Security** → **Code scanning**: lista os alertas encontrados pelo CodeQL. Para este projeto simples o esperado é **nenhum alerta**.
- Em **Artifacts**, o **`relatorio-sast`** contém o arquivo `csharp.sarif` (formato padrão de relatórios SAST, em JSON).

---

## Passo 7 — Ver a pipeline falhar (e corrigir)

O CI existe para **barrar** código quebrado. Vamos provar isso.

1. Em `LabDivisao.Tests/CalculadoraTests.cs`, troque o valor esperado de `5` para `6`:

   ```csharp
   Assert.Equal(6, resultado);
   ```

2. Envie a alteração:

   ```bash
   git add .
   git commit -m "Quebrando o teste de proposito"
   git push
   ```

3. Na aba **Actions**, a nova execução fica ❌ **vermelha** no step **Testes + coleta de cobertura**. Abra o log e veja a mensagem:

   ```text
   Assert.Equal() Failure: Values differ
   Expected: 6
   Actual:   5
   ```

4. Volte o valor para `5` e envie de novo:

   ```bash
   git add .
   git commit -m "Corrigindo o teste"
   git push
   ```

5. A pipeline volta a ficar ✅ **verde**.

---

## Problemas comuns

| Problema | Solução |
|---|---|
| A aba **Actions** não mostra nenhuma execução | O arquivo precisa estar **exatamente** em `.github/workflows/ci.yml` (com o ponto em `.github`) e a branch deve se chamar `main`. |
| `MSB1011: Specify which project or solution file to use` | Existe mais de um `.sln`/`.slnx`/`.csproj` na **raiz** do repositório. Deixe apenas o `LabDivisao.slnx`. |
| `Could not execute because the specified command or file was not found` no step do ReportGenerator | O `dotnet-tools.json` não foi enviado. Ele precisa estar na raiz do repositório. |
| `Invalid workflow file` / erro de YAML | Erro de indentação. Confira se usou **espaços** e se o arquivo está igual ao do Passo 4. |
| SAST falha com `Code scanning is not enabled` / `Advanced Security must be enabled` | O repositório é **privado**. Torne-o público (**Settings → General → Danger Zone → Change visibility**) **ou** adicione `upload: never` no step `Analisar o código` (o relatório continua disponível no artifact `relatorio-sast`). |
| `git push` rejeitado com `fetch first` | O repositório no GitHub não estava vazio. Rode `git pull origin main --allow-unrelated-histories` e depois `git push`. |
