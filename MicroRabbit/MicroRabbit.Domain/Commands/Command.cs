using MicroRabbit.Domain.Events;
using System;

namespace MicroRabbit.Domain.Commands
{
    public abstract class Command : Message
    {
        public DateTime Timestamp { get; protected set; }

        public Command() => Timestamp = DateTime.Now;
    }
}
