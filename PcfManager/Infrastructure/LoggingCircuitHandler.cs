using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PcfManager.Infrastructure
{
    public sealed class LoggingCircuitHandler : CircuitHandler
    {
        private readonly ILogger<LoggingCircuitHandler> _logger;
        private readonly IHttpContextAccessor _http;

        private sealed record SessionInfo(
            DateTimeOffset OpenedAtUtc,
            string UserName,
            string ClientIp,
            string UserAgent);

        private static readonly ConcurrentDictionary<string, SessionInfo> _sessions =
            new ConcurrentDictionary<string, SessionInfo>();

        public LoggingCircuitHandler(ILogger<LoggingCircuitHandler> logger, IHttpContextAccessor http)
        {
            _logger = logger;
            _http = http;
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var userName = "(anonymous)";
            var clientIp = "(unknown)";
            var userAgent = "(unknown)";

            try
            {
                var ctx = _http.HttpContext;
                if (ctx?.User?.Identity?.IsAuthenticated == true)
                    userName = ctx.User.Identity!.Name ?? "(no-name)";

                var ip = ctx?.Connection?.RemoteIpAddress;
                if (ip is not null)
                {
                    if (ip.IsIPv4MappedToIPv6)
                        ip = ip.MapToIPv4();
                    clientIp = ip.ToString();
                }

                if (ctx?.Request?.Headers.TryGetValue("User-Agent", out var ua) == true)
                    userAgent = ua.ToString();
            }
            catch
            {
                // Swallow: HttpContext can be unavailable beyond request scope.
            }

            var info = new SessionInfo(now, userName, clientIp, userAgent);
            _sessions[circuit.Id] = info;

            _logger.LogInformation(
                "BLZ CIRCUIT OPENED  id={CircuitId} user={User} ip={Ip} ua={UA} at={Utc:o}",
                circuit.Id, userName, clientIp, userAgent, now);

            return Task.CompletedTask;
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            if (_sessions.TryGetValue(circuit.Id, out var s))
            {
                _logger.LogInformation(
                    "BLZ CONNECTION UP   id={CircuitId} user={User} at={Utc:o}",
                    circuit.Id, s.UserName, DateTimeOffset.UtcNow);
            }
            else
            {
                _logger.LogInformation(
                    "BLZ CONNECTION UP   id={CircuitId} user=(unknown) at={Utc:o}",
                    circuit.Id, DateTimeOffset.UtcNow);
            }
            return Task.CompletedTask;
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            if (_sessions.TryGetValue(circuit.Id, out var s))
            {
                _logger.LogWarning(
                    "BLZ CONNECTION DOWN id={CircuitId} user={User} at={Utc:o} (sleep/network/recycle likely)",
                    circuit.Id, s.UserName, DateTimeOffset.UtcNow);
            }
            else
            {
                _logger.LogWarning(
                    "BLZ CONNECTION DOWN id={CircuitId} user=(unknown) at={Utc:o} (sleep/network/recycle likely)",
                    circuit.Id, DateTimeOffset.UtcNow);
            }
            return Task.CompletedTask;
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            if (_sessions.TryRemove(circuit.Id, out var s))
            {
                var duration = now - s.OpenedAtUtc;
                _logger.LogInformation(
                    "BLZ CIRCUIT CLOSED id={CircuitId} user={User} at={Utc:o} duration={Duration:c} ip={Ip} ua={UA}",
                    circuit.Id, s.UserName, now, duration, s.ClientIp, s.UserAgent);
            }
            else
            {
                _logger.LogInformation(
                    "BLZ CIRCUIT CLOSED id={CircuitId} user=(unknown) at={Utc:o}",
                    circuit.Id, now);
            }

            return Task.CompletedTask;
        }
    }
}
