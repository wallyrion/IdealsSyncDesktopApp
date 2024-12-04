using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorHybridApp.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorHybridApp.Core
{
    internal class BackgroundTest(IServiceProvider serviceProvider)
    {
        public async Task StartAsync()
        {
            while (true)
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Console.WriteLine("Something happened");
                await Task.Delay(1000);

                db.AppStates.Add(new AppState
                {
                    Key = $"Something{DateTime.UtcNow.ToShortTimeString()}",
                    Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                });

                await db.SaveChangesAsync();
            }
        }
    }
}
