using System;
using PRIMOX.Domain.Interfaces;

namespace PRIMOX.Domain.Tests.Fakes
{
    public class FakeTimeProvider : ITimeProvider
    {
        private DateTimeOffset _currentTime;

        public FakeTimeProvider(DateTimeOffset? initialTime = null)
        {
            _currentTime = initialTime ?? new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);
        }

        public DateTimeOffset GetUtcNow() => _currentTime;
        public DateTimeOffset GetLocalNow() => _currentTime;

        public void AdvanceTime(TimeSpan duration)
        {
            _currentTime = _currentTime.Add(duration);
        }

        public void SetTime(DateTimeOffset newTime)
        {
            _currentTime = newTime;
        }
    }
}
