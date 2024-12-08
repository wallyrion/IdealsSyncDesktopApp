namespace BlazorHybridApp.Core.Extensions;

public static class ServiceProviderExtensions
{
    public static AsyncServiceScope CreateDbContextScoped(this IServiceProvider serviceProvider, out AppDbContext dbContext)
    {
        var scope =  serviceProvider.CreateAsyncScope();
        dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return scope;
    }
}