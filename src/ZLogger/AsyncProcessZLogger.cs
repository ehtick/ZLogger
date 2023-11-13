﻿using Microsoft.Extensions.Logging;
using ZLogger.LogStates;

namespace ZLogger
{
    public sealed class AsyncProcessZLogger : ILogger
    {
        public IExternalScopeProvider? ScopeProvider { get; set; }

        readonly string categoryName;
        readonly IAsyncLogProcessor logProcessor;

        public AsyncProcessZLogger(string categoryName, IAsyncLogProcessor logProcessor)
        {
            this.categoryName = categoryName;
            this.logProcessor = logProcessor;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var info = new LogInfo(categoryName, DateTimeOffset.UtcNow, logLevel, eventId, exception);

            var scopeState = ScopeProvider != null
                ? LogScopeState.Create(ScopeProvider)
                : null;

            var entry = state is IZLoggerEntryCreatable
                ? ((IZLoggerEntryCreatable)state).CreateEntry(info) // constrained call avoiding boxing for value types
                : new StringFormatterLogState<TState>(state, exception, formatter).CreateEntry(info); // standard `log`

            entry.ScopeState = scopeState;

            if (state is IReferenceCountable)
            {
                ((IReferenceCountable)state).Retain();
            }

            logProcessor.Post(entry);
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return ScopeProvider?.Push(state) ?? NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        class NullDisposable : IDisposable
        {
            public static readonly IDisposable Instance = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }
}
