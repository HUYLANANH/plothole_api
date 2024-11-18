namespace PotholeDetectionApi.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class OtpCleanupService : IHostedService, IDisposable
    {
        private readonly EmailService _emailService;
        private Timer _timer;
        private readonly ILogger<OtpCleanupService> _logger;

        public OtpCleanupService(EmailService emailService, ILogger<OtpCleanupService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OTP Cleanup Service is starting.");

            _timer = new Timer(CleanupExpiredOtps, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        private void CleanupExpiredOtps(object state)
        {
            _logger.LogInformation("Running OTP cleanup task.");
            var currentTime = DateTime.Now;

            var expiredKeys = _emailService.GetExpiredKeys(currentTime);
            foreach (var key in expiredKeys)
            {
                try
                {
                    _emailService.RemoveOtp(key);
                    _logger.LogInformation($"Removed expired OTP for key: {key}");
                }catch (Exception ex) {
                    ex.ToString(); 
                }
                
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OTP Cleanup Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }

}
