using MicroRabbit.Domain.Commands;
using MicroRabbit.Domain.Events;
using System.Threading.Tasks;

namespace MicroRabbit.Domain.Bus
{
    public interface IEventBus
    {
        Task Send<T>(T command) where T : Command;

        void Publish<T>(T @event) where T : Event;

        void Subscribe<TEvent, THandler>()
            where TEvent : Event
            where THandler : IEventHandler<TEvent>;
    }
}
