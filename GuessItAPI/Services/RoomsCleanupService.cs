
using GuessItAPI.Interfaces;

namespace GuessItAPI.Services
{
    public class RoomsCleanupService : BackgroundService
    {
        private readonly IRoomStore _roomStore;
        private readonly ILogger<RoomsCleanupService> _logger;

        private readonly TimeSpan _cleanUpInterval = TimeSpan.FromSeconds(15);
        private readonly TimeSpan _roomTtl = TimeSpan.FromSeconds(60);

        public RoomsCleanupService(IRoomStore roomStore, ILogger<RoomsCleanupService> logger)
        {
            _roomStore = roomStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var removed = _roomStore.CleanupExpired(_roomTtl);
                if (removed > 0)
                    _logger.LogInformation($"Removed {removed} expired rooms");

                await Task.Delay(_cleanUpInterval, stoppingToken);
            }
        }
    }
}
