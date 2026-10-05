using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace api.Model
{
    // Runs the database clean-up every few hours (shared hosting has no SQL Agent to schedule it).
    // Keeps Retention:RawDays of per-entry readings and Retention:LogDays of log (appsettings.json).
    // Only does anything where migration 003 has been run, i.e. on the new host.
    public sealed class MaintenanceService : BackgroundService
    {
        private readonly IConfiguration configuration;

        public MaintenanceService(IConfiguration configuration) => this.configuration = configuration;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Let the app finish starting first.
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                int rawDays = configuration.GetValue("Retention:RawDays", 7);
                int logDays = configuration.GetValue("Retention:LogDays", 30);

                // Runs on a worker thread; RunMaintenance catches and logs its own errors.
                await Task.Run(() => Business.Reports.RunMaintenance(rawDays, logDays), stoppingToken);

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}
