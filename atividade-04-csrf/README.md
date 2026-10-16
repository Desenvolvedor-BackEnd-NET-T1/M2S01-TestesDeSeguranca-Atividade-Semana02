# Atividade 04 — CSRF (Cross-Site Request Forgery)

## Objetivo

Entender como funciona um ataque **CSRF**: um site malicioso faz o **seu navegador** enviar uma requisição para outro site **onde você está logado**, sem você perceber.

Nesta atividade o código **já está pronto** nesta pasta. Você vai:

1. Executar a aplicação vulnerável e fazer "login".
2. Abrir uma página maliciosa e ver o seu e-mail ser **trocado sozinho**.
3. Entender **por que** o ataque funciona (cookies e `SameSite`).
4. Aplicar a **correção** com token anti-forgery.
5. Repetir o ataque e ver que ele **falha**.

> ⚠️ Laboratório **local**, apenas para estudo. O "ataque" só troca um e-mail de teste por `atacante@lab.local`.

## O que você vai usar

| Ferramenta | Para quê |
|---|---|
| .NET 10 SDK | Executar a aplicação ASP.NET Core MVC |
| Certificado HTTPS de desenvolvimento do .NET | Rodar o site em `https://localhost` |
| Navegador **Chrome, Edge ou Firefox** | Executar o ataque |

> Funciona em **Windows, Linux e macOS**. No Windows use o **PowerShell**.

## Estrutura desta pasta

```text
atividade-04-csrf/
├── README.md
├── LabCsrf/                            ← site VÍTIMA (ASP.NET Core MVC)
│   ├── Program.cs
│   ├── Controllers/ContaController.cs  ← login falso, perfil e troca de e-mail
│   ├── Views/Conta/Perfil.cshtml       ← página do perfil
│   └── Properties/launchSettings.json  ← porta fixa https://localhost:7001
└── site-malicioso/
    └── ataque.html                     ← página do ATACANTE (fora do projeto)
```

---

## Passo 0 — Verificar o ambiente

```bash
dotnet --list-sdks
```

Deve aparecer uma linha começando com `10.`.

---

## Passo 1 — Como o projeto foi criado (referência)

> Você **não precisa** executar estes comandos: o projeto já está na pasta `LabCsrf`. Eles mostram como ele foi criado do zero.

```bash
dotnet new web -n LabCsrf -f net10.0 --use-program-main
cd LabCsrf
mkdir Controllers
mkdir Views
mkdir Views/Conta
```

Depois disso foram criados/alterados à mão:

| Arquivo | O que faz |
|---|---|
| `Program.cs` | Liga o MVC (`AddControllersWithViews`) e define a rota padrão `/{controller=Conta}/{action=Login}` |
| `Controllers/ContaController.cs` | Login falso, perfil e troca de e-mail |
| `Views/_ViewImports.cshtml` | Habilita as *Tag Helpers* nas Views |
| `Views/Conta/Perfil.cshtml` | Mostra o e-mail e o formulário de troca |
| `Properties/launchSettings.json` | Fixa a URL em `https://localhost:7001` |

> Usamos o template `web` (vazio) em vez de `mvc` para ter só o necessário e você conseguir ler todo o código.

---

## Passo 2 — Confiar no certificado HTTPS de desenvolvimento

O ataque precisa de **HTTPS** (o motivo está no [Passo 7](#passo-7--e-o-samesite)). Execute uma única vez na sua máquina:

```bash
dotnet dev-certs https --trust
```

| Sistema | O que acontece |
|---|---|
| Windows | Aparece uma janela pedindo confirmação. Clique em **Sim**. |
| macOS | Pede a senha do seu usuário. |
| Linux | Em algumas distribuições o navegador ainda mostra um aviso de certificado. Se aparecer, clique em **Avançado → Continuar para localhost**. Isso não atrapalha o lab. |

---

## Passo 3 — Entender o código vulnerável

Abra `LabCsrf/Controllers/ContaController.cs`. Os pontos importantes:

### 3.1 — Login falso (só grava um cookie)

```csharp
[HttpGet]
public IActionResult Login()
{
    Response.Cookies.Append(NomeCookie, "aluno", new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        // LAB: SameSite=None faz o navegador enviar o cookie mesmo em requisições
        // vindas de OUTRO site. É isso que permite reproduzir o CSRF.
        SameSite = SameSiteMode.None
    });

    return RedirectToAction(nameof(Perfil));
}
```

Não há senha. Acessar `/Conta/Login` grava o cookie `LabSessao=aluno` no navegador. A partir daí, o site considera você **logado**.

### 3.2 — Ação sensível SEM proteção

```csharp
// INTENCIONALMENTE VULNERÁVEL: não valida token anti-forgery.
[HttpPost]
public IActionResult AlterarEmail(string novoEmail)
{
    var usuario = Request.Cookies[NomeCookie];
    if (usuario is null || !_emails.ContainsKey(usuario))
    {
        return Unauthorized();
    }

    _emails[usuario] = novoEmail;
    return RedirectToAction(nameof(Perfil));
}
```

A **única** verificação é: "o cookie de sessão veio?". O servidor **não sabe** se a requisição saiu da página dele ou de qualquer outro site.

### 3.3 — A página do atacante

Abra `site-malicioso/ataque.html`:

```html
<form id="ataque" action="https://localhost:7001/Conta/AlterarEmail" method="post" style="display:none">
    <input type="hidden" name="novoEmail" value="atacante@lab.local" />
</form>

<script>
    document.getElementById("ataque").submit();
</script>
```

Um formulário **escondido**, apontando para o site vítima, que é **enviado sozinho** quando a página abre.

---

## Passo 4 — Executar o site vítima

Entre na pasta do projeto e execute:

```bash
cd LabCsrf
dotnet run --launch-profile https
```

Aguarde a mensagem:

```text
Now listening on: https://localhost:7001
```

Deixe esse terminal aberto.

---

## Passo 5 — Fazer "login" e ver o perfil

1. No navegador, abra **<https://localhost:7001/>**
2. Você é redirecionado para **/Conta/Perfil** e vê:

```text
Meu perfil
Usuário: aluno
E-mail atual: aluno@exemplo.com
```

3. (Opcional) Use o formulário **Trocar e-mail** para conferir que a troca legítima funciona.

> 💡 Aperte **F12** → aba **Application** (Chrome/Edge) ou **Storage** (Firefox) → **Cookies** → `https://localhost:7001`. Você verá o cookie `LabSessao` com `SameSite = None`.

---

## Passo 6 — Executar o ataque CSRF

**No mesmo navegador** em que você fez o login:

1. Abra o arquivo `site-malicioso/ataque.html`. Você pode:
   - dar **duplo clique** no arquivo (se o navegador padrão for o mesmo do login), **ou**
   - **arrastar** o arquivo para uma aba do navegador, **ou**
   - usar **Ctrl + O** (**Cmd + O** no macOS) e escolher o arquivo.
2. A página mostra "Parabéns! Você ganhou um prêmio!" por um instante e você é levado ao perfil.

**Resultado: ❌ o e-mail foi trocado sem você pedir.**

```text
E-mail atual: atacante@lab.local
```

Você **não clicou em nada** no site vítima. Só abriu uma outra página.

---

## Passo 7 — Por que o ataque funcionou?

### Por que o cookie foi enviado?

O navegador guarda os cookies **por site de destino**, não por site de origem. Quando **qualquer página** faz uma requisição para `https://localhost:7001`, o navegador pensa: *"tenho cookies para localhost:7001, vou anexá-los"*.

```text
 ataque.html (site do atacante)               localhost:7001 (site vítima)
 ─────────────────────────────                ──────────────────────────────
 <form> escondido + submit()   ──── POST ───► /Conta/AlterarEmail
                                  Cookie: LabSessao=aluno   ◄── anexado
                                  novoEmail=atacante@lab.local  automaticamente
                                                              pelo navegador
```

O servidor recebe um POST com um cookie válido. Para ele, é igual a uma requisição legítima. O atacante **não precisa ler** o cookie: basta fazer o navegador da vítima **enviá-lo**.

### E o SameSite?

O atributo `SameSite` do cookie diz ao navegador **quando** enviá-lo em requisições vindas de **outro site**:

| Valor | Requisição de outro site | Efeito no CSRF |
|---|---|---|
| `Strict` | Nunca envia o cookie | Bloqueia |
| `Lax` (**padrão** dos navegadores modernos) | Envia só em navegação **GET** de nível superior (clicar em um link). **Não envia em POST.** | Bloqueia este ataque (que é um POST) |
| `None` | Sempre envia. **Exige `Secure`** (só funciona em HTTPS) | Permite o ataque |

Por isso, **neste lab**:

- O cookie foi configurado com `SameSite = None` para o ataque ser reproduzível.
- O site roda em **HTTPS**, porque os navegadores **descartam** cookies `SameSite=None` sem o atributo `Secure`.

> ⚠️ **`SameSite=Lax` ajuda, mas não substitui o token anti-forgery.** Ele não protege ações sensíveis feitas via GET, não vale entre subdomínios do mesmo site (ex: `blog.empresa.com` → `app.empresa.com` são o "mesmo site") e depende do navegador do usuário.

### (Opcional) Experimento: ver o `Lax` bloqueando

1. Pare o servidor (`Ctrl + C`).
2. Em `ContaController.cs`, no método `Login`, troque `SameSite = SameSiteMode.None` por `SameSite = SameSiteMode.Lax`.
3. Rode de novo (`dotnet run --launch-profile https`), abra <https://localhost:7001/> e depois o `ataque.html`.
4. Resultado: **HTTP ERROR 401**. O navegador **não enviou** o cookie, então o servidor tratou a requisição como "não logado".
5. **Volte para `SameSite = SameSiteMode.None`** antes de seguir para o próximo passo.

---

## Passo 8 — Aplicar a correção (token anti-forgery)

Pare o servidor (`Ctrl + C`) e faça **duas** alterações.

### 8.1 — Validar o token no Controller

Em `LabCsrf/Controllers/ContaController.cs`, adicione `[ValidateAntiForgeryToken]` na ação `AlterarEmail`:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult AlterarEmail(string novoEmail)
```

### 8.2 — Gerar o token no formulário

Em `LabCsrf/Views/Conta/Perfil.cshtml`, adicione `@Html.AntiForgeryToken()` **dentro** do `<form>`:

```cshtml
<form action="/Conta/AlterarEmail" method="post">
    @Html.AntiForgeryToken()
    <input type="email" name="novoEmail" placeholder="novo@exemplo.com" required />
    <button type="submit">Salvar</button>
</form>
```

### Por que isso corrige?

`@Html.AntiForgeryToken()` coloca no formulário um campo escondido com um valor **aleatório e secreto**:

```html
<input name="__RequestVerificationToken" type="hidden" value="CfDJ8...(valor aleatório)..." />
```

Esse valor está **ligado** a um cookie de anti-forgery do usuário. O `[ValidateAntiForgeryToken]` exige que **os dois** cheguem juntos e combinem.

- A **sua página** consegue colocar o token no formulário, porque foi gerada pelo próprio servidor.
- A **página do atacante** não consegue: ela não tem como **ler** o HTML do site vítima (o navegador bloqueia isso pela *Same-Origin Policy*). Sem o token, a requisição é rejeitada.

O navegador continua enviando o cookie automaticamente, mas agora **cookie sozinho não basta**.

---

## Passo 9 — Testar o ataque novamente

1. Rode o servidor:

```bash
dotnet run --launch-profile https
```

2. Abra **<https://localhost:7001/>** (faz o login de novo; o e-mail volta a ser `aluno@exemplo.com`, pois tudo fica em memória).
3. Use o formulário **Trocar e-mail** normalmente. ✅ **Funciona**: o formulário legítimo envia o token.
4. Abra o `site-malicioso/ataque.html` no mesmo navegador.

**Resultado: ✅ o ataque falha com HTTP ERROR 400 (Bad Request).**

5. Volte para <https://localhost:7001/Conta/Perfil>. O e-mail **continua** o mesmo que você definiu.

> 💡 Para ver o token, clique com o botão direito no perfil → **Exibir código-fonte** e procure por `__RequestVerificationToken`.

---

## Resumo: como prevenir CSRF

| ✅ Faça | ❌ Não faça |
|---|---|
| Use `[ValidateAntiForgeryToken]` em toda ação que **altera** dados (ou o filtro global `AutoValidateAntiforgeryTokenAttribute`) | Confiar apenas no cookie de sessão para autorizar ações |
| Gere o token nos formulários (`@Html.AntiForgeryToken()` ou `<form asp-action="...">`, que gera sozinho) | Fazer alterações de dados com **GET** |
| Mantenha cookies de sessão com `SameSite=Lax` ou `Strict` | Usar `SameSite=None` sem necessidade real |
| Use `HttpOnly` e `Secure` nos cookies de sessão | Achar que HTTPS sozinho protege contra CSRF |

## Problemas comuns

| Problema | Solução |
|---|---|
| Aviso "Sua conexão não é particular" | Rode `dotnet dev-certs https --trust` (Passo 2) ou clique em **Avançado → Continuar para localhost**. |
| O ataque deu **HTTP 401** sem eu ter corrigido nada | O cookie não foi enviado. Confira: (1) fez o login no **mesmo navegador** e **mesma janela** (não anônima)? (2) o código está com `SameSite = SameSiteMode.None`? (3) não está usando Safari? Prefira Chrome, Edge ou Firefox. |
| Página em branco ou erro ao abrir o `ataque.html` | Abra o arquivo direto do disco (duplo clique, arrastar ou Ctrl+O). A URL deve começar com `file://`. |
| `address already in use` na porta 7001 | Outro programa usa a porta. Troque `7001` em `Properties/launchSettings.json` **e** em `site-malicioso/ataque.html`. |
| A troca legítima também deu HTTP 400 depois da correção | Faltou o `@Html.AntiForgeryToken()` **dentro** do `<form>` em `Perfil.cshtml`. |
