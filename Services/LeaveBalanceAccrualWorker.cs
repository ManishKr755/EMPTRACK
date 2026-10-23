using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EMPTRACK.Services;

/// <summary>Periodically catches up monthly and annual leave allocations.</summary>
public sealed class LeaveBalanceAccrualWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaveBalanceAccrualWorker> _logger;

    public LeaveBalanceAccrualWorker(IServiceScopeFactory scopeFactory, ILogger<LeaveBalanceAccrualWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<LeaveBalanceService>();
                await service.EnsureAndAccrueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update EMPTRACK leave balances.");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
