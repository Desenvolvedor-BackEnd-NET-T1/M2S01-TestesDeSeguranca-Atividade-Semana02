namespace LabCsrf;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Habilita Controllers + Views Razor (padrão MVC).
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        app.UseRouting();

        // Rota padrão: /{controller}/{action}. A raiz "/" abre a tela de login.
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Conta}/{action=Login}");

        app.Run();
    }
}
