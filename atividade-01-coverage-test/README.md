# Atividade 01 — TDD e Cobertura de Testes

## Objetivo

Escrever um teste **antes** do código (TDD), fazer o teste passar e gerar o **relatório de cobertura de testes**.

No final você terá um console application em .NET 10 que divide dois números, com o código nascido a partir do teste.

## O que você vai usar

| Ferramenta | Para quê |
|---|---|
| .NET 10 SDK | Criar, compilar e executar os projetos |
| xUnit | Framework de testes |
| coverlet | Coletar a cobertura enquanto os testes rodam |
| ReportGenerator | Transformar a cobertura em um relatório HTML |

> Funciona em **Windows, Linux e macOS**. Todos os comandos `dotnet` são iguais nos três sistemas.
> Quando houver diferença, ela estará indicada.

---

## Passo 0 — Verificar o ambiente

Abra um terminal (no Windows: **PowerShell**) e execute:

```bash
dotnet --list-sdks
```

Deve aparecer pelo menos uma linha começando com `10.`. Se não aparecer, instale o .NET 10 SDK em <https://dotnet.microsoft.com/download/dotnet/10.0>.

---

## Passo 1 — Criar a estrutura da solução

Crie uma pasta para o laboratório e entre nela:

```bash
mkdir LabDivisao
cd LabDivisao
```

Crie a solução, o projeto de testes (xUnit) e o projeto console:

```bash
dotnet new sln -n LabDivisao
dotnet new xunit -n LabDivisao.Tests -f net10.0
dotnet new console -n LabDivisao.App -f net10.0 --use-program-main
```

> **Por que `--use-program-main`?** Gera o `Program.cs` no estilo clássico (*legacy style*), com `namespace`, `class Program` e `static void Main`, em vez de *top-level statements*.

Adicione os projetos na solução e faça o projeto de testes **enxergar** o projeto console:

```bash
dotnet sln add LabDivisao.Tests/LabDivisao.Tests.csproj LabDivisao.App/LabDivisao.App.csproj
dotnet add LabDivisao.Tests/LabDivisao.Tests.csproj reference LabDivisao.App/LabDivisao.App.csproj
```

Estrutura esperada:

```text
LabDivisao/
├── LabDivisao.slnx
├── LabDivisao.App/
│   ├── LabDivisao.App.csproj
│   └── Program.cs
└── LabDivisao.Tests/
    ├── LabDivisao.Tests.csproj
    └── UnitTest1.cs
```

> O template `xunit` já vem com o pacote **`coverlet.collector`** instalado. Confira abrindo `LabDivisao.Tests/LabDivisao.Tests.csproj`.

---

## Passo 2 — Escrever o teste primeiro (🔴 RED)

No TDD o teste vem **antes** do código. Ele descreve o comportamento que queremos.

1. Apague o arquivo de exemplo `LabDivisao.Tests/UnitTest1.cs`.
2. Crie o arquivo `LabDivisao.Tests/CalculadoraTests.cs` com o conteúdo:

```csharp
namespace LabDivisao.Tests;

using LabDivisao.App;

public class CalculadoraTests
{
    [Fact]
    public void Dividir_DeveRetornarOResultadoDaDivisao_QuandoReceberDoisNumeros()
    {
        // Arrange
        var calculadora = new Calculadora();

        // Act
        var resultado = calculadora.Dividir(10, 2);

        // Assert
        Assert.Equal(5, resultado);
    }
}
```

Execute os testes:

```bash
dotnet test
```

**Resultado esperado: ERRO de compilação.**

```text
error CS0246: O nome do tipo ou do namespace "Calculadora" não pode ser encontrado
```

✅ Isso é o esperado! A classe `Calculadora` ainda não existe. Essa é a fase **RED** do TDD: o teste falha porque o código ainda não foi escrito.

---

## Passo 3 — Escrever o código mínimo para passar (🟢 GREEN)

Crie o arquivo `LabDivisao.App/Calculadora.cs`:

```csharp
namespace LabDivisao.App;

public class Calculadora
{
    public decimal Dividir(decimal dividendo, decimal divisor)
    {
        return dividendo / divisor;
    }
}
```

> A classe precisa ser `public` para o projeto de testes conseguir usá-la.

Execute os testes novamente:

```bash
dotnet test
```

**Resultado esperado:**

```text
Aprovado!  – Com falha:     0, Aprovado:     1, Ignorado:     0, Total:     1
```

(Em inglês: `Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1`)

✅ Fase **GREEN**: o teste passou.

---

## Passo 4 — Gerar a cobertura de testes

### 4.1 — Coletar a cobertura com o coverlet

```bash
dotnet test --collect:"XPlat Code Coverage"
```

No final da saída aparece o caminho de um arquivo **`coverage.cobertura.xml`**, parecido com:

```text
Anexos:
  .../LabDivisao.Tests/TestResults/<um-guid>/coverage.cobertura.xml
```

Esse XML é a cobertura "crua". Ele não é fácil de ler, por isso vamos gerar um relatório HTML.

### 4.2 — Instalar o ReportGenerator (como ferramenta local)

Ainda na pasta `LabDivisao`, execute:

```bash
dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool
```

> **Por que ferramenta local?** Ela fica registrada no arquivo `dotnet-tools.json` do projeto. Não depende do `PATH` do sistema e qualquer pessoa (ou o CI) pode instalar a mesma versão com `dotnet tool restore`.

### 4.3 — Gerar o relatório HTML

```bash
dotnet reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;TextSummary"
```

### 4.4 — Abrir o relatório

| Sistema | Comando |
|---|---|
| Windows (PowerShell) | `start coveragereport/index.html` |
| macOS | `open coveragereport/index.html` |
| Linux | `xdg-open coveragereport/index.html` |

Ou veja o resumo direto no terminal:

| Sistema | Comando |
|---|---|
| Windows (PowerShell) | `Get-Content coveragereport/Summary.txt` |
| macOS / Linux | `cat coveragereport/Summary.txt` |

**O que observar no relatório:**

```text
LabDivisao.App                   50%
  LabDivisao.App.Calculadora    100%
  LabDivisao.App.Program          0%
```

- `Calculadora` está com **100%**: todas as linhas foram executadas pelo teste.
- `Program` está com **0%**: nenhum teste executa o `Main`.

> 💡 Clique em `LabDivisao.App.Calculadora` no HTML para ver as linhas pintadas de **verde** (executadas) e **vermelho** (não executadas).

---

## Passo 5 — Criar o console application

Agora que a regra de negócio está testada, vamos usá-la no programa.

Substitua todo o conteúdo de `LabDivisao.App/Program.cs` por:

```csharp
namespace LabDivisao.App;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Digite o dividendo: ");
        var dividendo = decimal.Parse(Console.ReadLine()!);

        Console.Write("Digite o divisor: ");
        var divisor = decimal.Parse(Console.ReadLine()!);

        var calculadora = new Calculadora();
        var resultado = calculadora.Dividir(dividendo, divisor);

        Console.WriteLine($"Resultado: {resultado}");
    }
}
```

Execute o programa:

```bash
dotnet run --project LabDivisao.App
```

Exemplo de uso:

```text
Digite o dividendo: 10
Digite o divisor: 4
Resultado: 2,5
```

> O separador decimal segue o idioma do seu sistema: `2,5` em português, `2.5` em inglês. Para digitar números quebrados use o mesmo separador.

---

## Passo 6 — Gerar a cobertura novamente

Apague os resultados antigos (para não misturar com a execução anterior):

| Sistema | Comando |
|---|---|
| Windows (PowerShell) | `Remove-Item -Recurse -Force LabDivisao.Tests/TestResults, coveragereport` |
| macOS / Linux | `rm -rf LabDivisao.Tests/TestResults coveragereport` |

Gere tudo de novo:

```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;TextSummary"
```

**O que mudou?** A cobertura geral **caiu** (de 50% para 25%), porque o `Program` agora tem mais linhas e nenhuma delas é testada. A `Calculadora` continua com 100%.

> 💡 **Reflexão:** cobertura mede *quais linhas foram executadas* pelos testes, não *se o código está correto*. Por exemplo: o que acontece se o divisor for `0`? Nenhum teste cobre esse cenário, mesmo com 100% na `Calculadora`.

---

## Resumo dos comandos

```bash
# Estrutura
dotnet new sln -n LabDivisao
dotnet new xunit -n LabDivisao.Tests -f net10.0
dotnet new console -n LabDivisao.App -f net10.0 --use-program-main
dotnet sln add LabDivisao.Tests/LabDivisao.Tests.csproj LabDivisao.App/LabDivisao.App.csproj
dotnet add LabDivisao.Tests/LabDivisao.Tests.csproj reference LabDivisao.App/LabDivisao.App.csproj

# Testes
dotnet test

# Cobertura
dotnet test --collect:"XPlat Code Coverage"
dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool
dotnet reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;TextSummary"

# Console
dotnet run --project LabDivisao.App
```

## Problemas comuns

| Problema | Solução |
|---|---|
| `error CS0246 ... Calculadora` depois do Passo 3 | Confira se a classe é `public` e se o `namespace` é `LabDivisao.App`. |
| `error CS0246 ... Calculadora` mesmo com a classe certa | Faltou a referência entre projetos: rode o `dotnet add ... reference ...` do Passo 1. |
| `Could not execute because the specified command or file was not found` ao rodar `dotnet reportgenerator` | Você está fora da pasta `LabDivisao` ou pulou o Passo 4.2. |
| Relatório mostra dados antigos | Apague `TestResults` e `coveragereport` (Passo 6) e gere de novo. |
| `FormatException` ao rodar o console | Digite apenas números, usando o separador decimal do seu sistema. |

> **Guarde este projeto!** Ele será usado na **Atividade 05 (CI)**.
