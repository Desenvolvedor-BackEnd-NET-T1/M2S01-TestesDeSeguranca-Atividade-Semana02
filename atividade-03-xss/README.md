# Atividade 03 — XSS (Cross-Site Scripting)

## Objetivo

Simular um ataque de **XSS armazenado** (*Stored XSS*) em uma página de comentários e aplicar a **correção**.

Você vai:

1. Criar um servidor web com **Razor Pages**.
2. Criar uma página de comentários **vulnerável**.
3. Publicar um comentário com código JavaScript e ver o código **executar** no navegador.
4. Corrigir a página.
5. Repetir o ataque e ver que **nada acontece**.

> ⚠️ Laboratório **local**, apenas para estudo. Use somente payloads inofensivos como `alert(...)`.

## O que você vai usar

| Ferramenta | Para quê |
|---|---|
| .NET 10 SDK | Criar e executar o servidor web |
| Razor Pages | Páginas HTML geradas no servidor com C# |
| Navegador (Chrome, Edge ou Firefox) | Executar o ataque |

> Funciona em **Windows, Linux e macOS**. No Windows use o **PowerShell**.

---

## Passo 0 — Verificar o ambiente

```bash
dotnet --list-sdks
```

Deve aparecer uma linha começando com `10.`.

---

## Passo 1 — Criar o projeto Razor Pages

```bash
dotnet new webapp -n LabXss -f net10.0 --use-program-main
cd LabXss
```

- `webapp`: template de **Razor Pages**.
- `--use-program-main`: gera o `Program.cs` no estilo clássico (*legacy style*).

---

## Passo 2 — Criar o "back-end" da página de comentários

Crie o arquivo `Pages/Comentarios.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LabXss.Pages;

public class ComentariosModel : PageModel
{
    // Armazenamento em memória (apenas para o laboratório).
    private static readonly List<string> _comentarios = new List<string>();

    [BindProperty]
    public string Texto { get; set; } = string.Empty;

    public List<string> Comentarios => _comentarios;

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!string.IsNullOrWhiteSpace(Texto))
        {
            _comentarios.Add(Texto);
        }

        return RedirectToPage();
    }
}
```

**Por que assim?**

- A lista é `static`: os comentários ficam guardados na memória enquanto o servidor estiver rodando. Todo mundo que abrir a página vê os mesmos comentários (como em um blog real).
- `OnPost` salva **exatamente** o que o usuário digitou, sem nenhum tratamento.

---

## Passo 3 — Criar a página HTML VULNERÁVEL

Crie o arquivo `Pages/Comentarios.cshtml`:

```cshtml
@page
@model ComentariosModel
@{
    ViewData["Title"] = "Comentários";
}

<h1>Deixe seu comentário</h1>

<form method="post">
    <textarea asp-for="Texto" rows="3" cols="60"></textarea>
    <br />
    <button type="submit">Enviar</button>
</form>

<hr />

<h2>Comentários publicados</h2>

<ul>
    @foreach (var comentario in Model.Comentarios)
    {
        <!-- VULNERÁVEL: Html.Raw desliga a proteção automática do Razor -->
        <li>@Html.Raw(comentario)</li>
    }
</ul>
```

> **Onde está a falha?** `@Html.Raw(...)` escreve o texto no HTML **como está**. Se o texto tiver tags HTML ou `<script>`, o navegador vai interpretá-las como código.

---

## Passo 4 — Executar o servidor

```bash
dotnet run --urls http://localhost:5090
```

Aguarde a mensagem:

```text
Now listening on: http://localhost:5090
```

> Pode aparecer um aviso `Failed to determine the https port for redirect`. Ele é esperado, pois o lab roda apenas em HTTP. Pode ignorar.

Abra no navegador: **<http://localhost:5090/Comentarios>**

Faça um teste normal: digite `Olá, primeiro comentário!` e clique em **Enviar**. O comentário aparece na lista. ✅

---

## Passo 5 — Reproduzir o ataque XSS

No campo de comentário, digite **exatamente**:

```html
<script>alert('XSS')</script>
```

Clique em **Enviar**.

**Resultado: ❌ aparece uma janela de alerta com o texto `XSS`.**

Agora **recarregue a página** (F5) ou abra a mesma URL em outra aba. O alerta aparece **de novo**. O código ficou **armazenado** no servidor e executa para **qualquer pessoa** que abrir a página. Por isso se chama **Stored XSS**.

Faça mais um teste com outro payload:

```html
<img src="x" onerror="alert('XSS via imagem')">
```

> 💡 Clique com o botão direito na página → **Exibir código-fonte**. Você verá o seu `<script>` dentro do HTML, exatamente como foi digitado:
>
> ```html
> <li><script>alert('XSS')</script></li>
> ```

**Por que isso é grave?** No lugar de um `alert`, um atacante poderia:

- Roubar dados da página ou cookies que não sejam `HttpOnly`;
- Fazer requisições em nome do usuário logado;
- Trocar o conteúdo da página (ex: um formulário de login falso).

---

## Passo 6 — Aplicar a correção

Pare o servidor com `Ctrl + C`.

No arquivo `Pages/Comentarios.cshtml`, troque o trecho:

```cshtml
        <!-- VULNERÁVEL: Html.Raw desliga a proteção automática do Razor -->
        <li>@Html.Raw(comentario)</li>
```

por:

```cshtml
        <!-- CORRIGIDO: o Razor codifica o HTML automaticamente -->
        <li>@comentario</li>
```

**Por que isso corrige?** Quando você escreve `@variavel`, o Razor faz o **HTML encoding** automaticamente. Os caracteres especiais viram texto inofensivo:

| Caractere | Vira |
|---|---|
| `<` | `&lt;` |
| `>` | `&gt;` |
| `'` | `&#x27;` |
| `"` | `&quot;` |

O navegador **mostra** esses caracteres na tela, mas **não executa** nada.

---

## Passo 7 — Tentar o ataque novamente

Execute o servidor de novo:

```bash
dotnet run --urls http://localhost:5090
```

> Como os comentários ficam em memória, ao reiniciar o servidor a lista começa vazia.

Abra **<http://localhost:5090/Comentarios>** e envie os mesmos payloads:

```html
<script>alert('XSS')</script>
```

```html
<img src="x" onerror="alert('XSS via imagem')">
```

**Resultado: ✅ nenhum alerta aparece.** O comentário é exibido como **texto**, exatamente como foi digitado.

No **código-fonte** da página agora aparece:

```html
<li>&lt;script&gt;alert(&#x27;XSS&#x27;)&lt;/script&gt;</li>
```

---

## Resumo: como prevenir XSS

| ✅ Faça | ❌ Não faça |
|---|---|
| Exiba dados do usuário com `@variavel` (Razor codifica sozinho) | Usar `@Html.Raw(...)` com dados vindos do usuário |
| Se precisar aceitar HTML, use uma biblioteca de **sanitização** (ex: `HtmlSanitizer`) | Tentar bloquear `<script>` "na mão" com `Replace` (existem muitas outras formas de injetar código) |
| Marque cookies de sessão como `HttpOnly` | Deixar cookies sensíveis acessíveis via JavaScript |
| Use **Content-Security-Policy (CSP)** como camada extra de defesa | Confiar em uma única camada de proteção |

## Problemas comuns

| Problema | Solução |
|---|---|
| A página `/Comentarios` retorna 404 | Confira se os arquivos estão na pasta `Pages/` e se a primeira linha do `.cshtml` é `@page`. |
| Fiz a correção mas o alerta continua | Você não reiniciou o servidor. Pare com `Ctrl + C` e rode `dotnet run` de novo. |
| `HTTP 400` ao enviar o comentário | O formulário precisa ter `method="post"` e ser enviado pela própria página (o Razor adiciona um token de segurança automaticamente). |
| `address already in use` | A porta 5090 está ocupada. Use outra, ex: `--urls http://localhost:5091`. |
