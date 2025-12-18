namespace NewRecap.Services
{
    public class WeeklyReportBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private DateTime _lastRunDate = DateTime.MinValue;

        public WeeklyReportBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var today = DateTime.Today;

                if (today.DayOfWeek == DayOfWeek.Monday && _lastRunDate != today)
                {
                    using var scope = _scopeFactory.CreateScope();

                    GenerateWeeklyReports(scope);

                    _lastRunDate = today;
                }

                // Check once per hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private void GenerateWeeklyReports(IServiceScope scope)
        {
            DateTime weekStart = DateTime.Today;

            // Ensure Monday is the start
            if (weekStart.DayOfWeek != DayOfWeek.Monday)
            {
                weekStart = weekStart.AddDays(-(int)weekStart.DayOfWeek + (int)DayOfWeek.Monday);
            }
        }

    }// End of 'WeeklyReportBackgroundService' Class.
}// End of 'namespace'.
