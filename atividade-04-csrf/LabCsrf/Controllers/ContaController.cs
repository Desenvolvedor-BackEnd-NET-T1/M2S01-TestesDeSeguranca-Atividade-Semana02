using Microsoft.AspNetCore.Mvc;

namespace LabCsrf.Controllers;

public class ContaController : Controller
{
    private const string NomeCookie = "LabSessao";

    // "Banco de dados" em memória: usuário -> e-mail (apenas para o laboratório).
    private static readonly Dictionary<string, string> _emails = new Dictionary<string, string>
    {
        ["aluno"] = "aluno@exemplo.com"
    };

    // GET /Conta/Login
    // Login FALSO: não pede senha, apenas grava o cookie de sessão no navegador.
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

    // GET /Conta/Perfil
    // Mostra o e-mail do usuário "logado".
    [HttpGet]
    public IActionResult Perfil()
    {
        var usuario = Request.Cookies[NomeCookie];
        if (usuario is null || !_emails.ContainsKey(usuario))
        {
            return RedirectToAction(nameof(Login));
        }

        ViewBag.Usuario = usuario;
        ViewBag.Email = _emails[usuario];
        return View();
    }

    // POST /Conta/AlterarEmail
    // INTENCIONALMENTE VULNERÁVEL: não valida token anti-forgery.
    // Basta o navegador enviar o cookie de sessão para a alteração ser aceita.
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

    // GET /Conta/Sair
    [HttpGet]
    public IActionResult Sair()
    {
        Response.Cookies.Delete(NomeCookie, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return Content("Sessão encerrada.");
    }
}
