using MicroRabbit.Domain.Events;
using System.Threading.Tasks;

namespace MicroRabbit.Domain.Bus
{
    public interface IEventHandler
    {
    }

    public interface IEventHandler<in T> : IEventHandler where T : Event
    {
        Task Handle(T @event);
    }
}
