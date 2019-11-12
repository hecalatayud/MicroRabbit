using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using MicroRabbit.Domain.Bus;
using MicroRabbit.Domain.Commands;
using MicroRabbit.Domain.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroRabbit.Infrastructure.Bus
{
    public sealed class RabbitMqBus : IEventBus
    {
        private readonly IMediator _mediator;
        private readonly IDictionary<string, ICollection<Type>> _handlers;
        private readonly ICollection<Type> _eventTypes;

        public RabbitMqBus(IMediator mediator)
        {
            _mediator = mediator;
            _handlers = new Dictionary<string, ICollection<Type>>();
            _eventTypes = new List<Type>();
        }

        public Task Send<T>(T command) where T : Command => _mediator.Send(command);

        public void Publish<T>(T @event) where T : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var queue = @event.GetType().Name;

                channel.QueueDeclare(queue, false, false, false, null);
                channel.BasicPublish(string.Empty, queue, null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)));
            }
        }

        public void Subscribe<TEvent, THandler>()
            where TEvent : Event
            where THandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var eventName = eventType.Name;
            var handlerType = typeof(THandler);

            if (!_handlers.ContainsKey(eventName))
                _handlers.Add(eventName, new List<Type>());

            if (!_eventTypes.Contains(eventType))
                _eventTypes.Add(eventType);

            if (_handlers[eventName].Any(type => type.GetType() == handlerType))
                throw new ArgumentException($"Handler of type '{handlerType}' is already registered for {eventName} event", nameof(handlerType));

            _handlers[eventName].Add(handlerType);

            StartBasicConsume<TEvent>();
        }

        private void StartBasicConsume<TEvent>() where TEvent : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var queue = typeof(TEvent).Name;
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.Received += Consumer_Received;

                channel.QueueDeclare(queue, false, false, false, null);
                channel.BasicConsume(queue, true, consumer);
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                await ProcessEvent(@event.RoutingKey, Encoding.UTF8.GetString(@event.Body)).ConfigureAwait(false);
            }

            catch (Exception e)
            {

            }
        }

        private async Task ProcessEvent(string eventName, string body)
        {
            if (!_handlers.ContainsKey(eventName))
                return;

            foreach (var subscription in _handlers[eventName])
            {
                var handler = Activator.CreateInstance(subscription);

                if (handler == null)
                    continue;

                var eventType = _eventTypes.Single(type => type.Name == eventName);

                await (Task) typeof(IEventHandler<>).MakeGenericType(eventType).GetMethod(nameof(IEventHandler<Event>.Handle)).Invoke(
                    handler,
                    new object[]
                    {
                        JsonConvert.DeserializeObject(body, eventType)
                    });
            }
        }
    }
}
