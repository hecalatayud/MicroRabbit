using MediatR;
using System;

namespace MicroRabbit.Domain.Events
{
    public abstract class Message : IRequest<bool>
    {
        public Type Type { get; protected set; }

        public Message() => Type = GetType();
    }
}
