using System;
using System.Reactive.Subjects;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager
{
    /// <summary>
    /// A simple publish-subscribe event bus built on top of Reactive Extensions.
    /// Components can publish <see cref="ServiceEvent"/> notifications and subscribe
    /// to a filtered stream of events.
    /// </summary>
    public class EventBus : IDisposable
    {
        private readonly Subject<ServiceEvent> _subject = new Subject<ServiceEvent>();
        private bool _disposed;

        /// <summary>Publishes a <see cref="ServiceEvent"/> to all current subscribers.</summary>
        public void Publish(ServiceEvent serviceEvent)
        {
            if (_disposed) return;
            if (serviceEvent == null) throw new ArgumentNullException(nameof(serviceEvent));
            _subject.OnNext(serviceEvent);
        }

        /// <summary>Returns an observable stream of all service events.</summary>
        public IObservable<ServiceEvent> GetEvents() => _subject;

        /// <summary>Returns an observable stream filtered to a specific service.</summary>
        public IObservable<ServiceEvent> GetEventsFor(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("Service name cannot be empty.", nameof(serviceName));
            return System.Reactive.Linq.Observable.Where(_subject, e => e.ServiceName == serviceName);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _subject.OnCompleted();
                _subject.Dispose();
            }
        }
    }
}
