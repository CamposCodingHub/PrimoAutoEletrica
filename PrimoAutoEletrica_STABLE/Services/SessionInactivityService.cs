using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrimoAutoEletrica.Services
{
    public sealed class SessionInactivityEventArgs : EventArgs
    {
        public DateTime LastActivityAt { get; init; }
        public DateTime ExpiredAt { get; init; }
        public TimeSpan IdleFor { get; init; }
        public TimeSpan Timeout { get; init; }
    }

    /// <summary>
    /// Monitora interacoes globais da UI e expira a sessao apos um periodo de inatividade.
    /// </summary>
    public sealed class SessionInactivityService : IDisposable
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(30);

        private readonly AppSessionService _session;
        private readonly LoggerService _logger;
        private readonly DispatcherTimer _timer;
        private readonly TimeSpan _timeout;
        private bool _monitorando;
        private bool _expirado;

        public event EventHandler<SessionInactivityEventArgs>? SessionExpired;

        public SessionInactivityService(
            AppSessionService session,
            LoggerService logger,
            TimeSpan? timeout = null,
            TimeSpan? pollingInterval = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeout = timeout ?? DefaultTimeout;
            _timer = new DispatcherTimer
            {
                Interval = pollingInterval ?? TimeSpan.FromMinutes(1)
            };
            _timer.Tick += OnTimerTick;
        }

        public TimeSpan Timeout => _timeout;

        public void Start()
        {
            if (_monitorando || !_session.IsAuthenticated)
            {
                return;
            }

            _expirado = false;
            _session.TouchActivity();
            InputManager.Current.PreProcessInput += OnPreProcessInput;
            _timer.Start();
            _monitorando = true;
            _logger.LogInfo($"Monitor de inatividade iniciado com timeout de {_timeout.TotalMinutes:F0} minutos.");
        }

        public void Stop()
        {
            if (!_monitorando)
            {
                return;
            }

            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            _timer.Stop();
            _monitorando = false;
            _expirado = false;
        }

        public void Dispose()
        {
            Stop();
            _timer.Tick -= OnTimerTick;
        }

        private void OnPreProcessInput(object? sender, PreProcessInputEventArgs e)
        {
            if (_expirado || !_session.IsAuthenticated)
            {
                return;
            }

            if (!EhInputRelevante(e.StagingItem.Input))
            {
                return;
            }

            _session.TouchActivity();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_expirado || !_session.IsAuthenticated)
            {
                return;
            }

            var ultimaAtividade = _session.LastActivityAt ?? _session.LoginAt ?? DateTime.Now;
            var tempoInativo = DateTime.Now - ultimaAtividade;

            if (tempoInativo < _timeout)
            {
                return;
            }

            _expirado = true;
            Stop();

            _logger.LogWarning($"Sessao expirada por inatividade. Usuario='{_session.UserEmail}', InativoPor='{tempoInativo}'.");
            SessionExpired?.Invoke(this, new SessionInactivityEventArgs
            {
                LastActivityAt = ultimaAtividade,
                ExpiredAt = DateTime.Now,
                IdleFor = tempoInativo,
                Timeout = _timeout
            });
        }

        private static bool EhInputRelevante(InputEventArgs? input)
        {
            return input switch
            {
                KeyboardEventArgs => true,
                MouseEventArgs => true,
                StylusEventArgs => true,
                TextCompositionEventArgs => true,
                _ => false
            };
        }
    }
}
