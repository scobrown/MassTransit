// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Transports.RabbitMq.Management
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using RabbitMQ.Client;

    public class RabbitMqEndpointManagement :
        IRabbitMqEndpointManagement
    {
        static readonly ILog _log = Logger.Get(typeof (RabbitMqEndpointManagement));
        readonly IRabbitMqEndpointAddress _address;
        readonly bool _owned;
        IConnection _connection;
        bool _disposed;

        public RabbitMqEndpointManagement(IRabbitMqEndpointAddress address)
            : this(address, address.CreateConnection())
        {
            _owned = true;
        }

        public RabbitMqEndpointManagement(IRabbitMqEndpointAddress address, IConnection connection)
        {
            _address = address;
            _connection = connection;
        }

        public void BindQueue(string queueName, string exchangeName, string exchangeType, string routingKey,
                              IDictionary queueArguments)
        {
            using (IModel model = _connection.CreateModel())
            {
                string queue = model.QueueDeclare(queueName, true, false, false, queueArguments);
                model.ExchangeDeclare(exchangeName, exchangeType, true);

                model.QueueBind(queue, exchangeName, routingKey);

                model.Close(200, "ok");
            }
        }

        public void UnbindQueue(string queueName, string exchangeName, string routingKey)
        {
            using (IModel model = _connection.CreateModel())
            {
                model.QueueUnbind(queueName, exchangeName, routingKey, null);

                model.Close(200, "ok");
            }
        }

        public void BindExchange(string destination, string source, string exchangeType, string routingKey)
        {
            using (IModel model = _connection.CreateModel())
            {
                model.ExchangeDeclare(destination, exchangeType, true, false, null);
                model.ExchangeDeclare(source, exchangeType, true, false, null);

                model.ExchangeBind(destination, source, routingKey);

                model.Close(200, "ok");
            }
        }

        public void UnbindExchange(string destination, string source, string routingKey)
        {
            using (IModel model = _connection.CreateModel())
            {
                model.ExchangeUnbind(destination, source, routingKey, null);

                model.Close(200, "ok");
            }
        }

        public void Purge(string queueName)
        {
            using (IModel model = _connection.CreateModel())
            {
                try
                {
                    model.QueueDeclarePassive(queueName);
                    model.QueuePurge(queueName);
                }
                catch
                {
                }

                model.Close(200, "purged queue");
            }
        }

        public IEnumerable<Type> BindExchangesForPublisher(Type messageType, IMessageNameFormatter messageNameFormatter)
        {
            MessageName messageName = messageNameFormatter.GetMessageName(messageType);

            using (IModel model = _connection.CreateModel())
            {
                model.ExchangeDeclare(messageName.ToString(), ExchangeType.Fanout, true, false, null);

                yield return messageType;

                foreach (Type type in messageType.GetMessageTypes().Skip(1))
                {
                    MessageName interfaceName = messageNameFormatter.GetMessageName(type);

                    model.ExchangeDeclare(interfaceName.ToString(), ExchangeType.Fanout, true, false, null);
                    model.ExchangeBind(interfaceName.ToString(), messageName.ToString(), "");

                    yield return type;
                }

                model.Close(200, "ok");
            }
        }

        public void BindExchangesForSubscriber(Type messageType, IMessageNameFormatter messageNameFormatter)
        {
            MessageName messageName = messageNameFormatter.GetMessageName(messageType);

            BindExchange(_address.Name, messageName.ToString(), ExchangeType.Fanout, "");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                if (_connection != null)
                {
                    if (_owned)
                    {
                        if (_connection.IsOpen)
                            _connection.Close(200, "normal");
                        _connection.Dispose();
                    }

                    _connection = null;
                }
            }

            _disposed = true;
        }

        ~RabbitMqEndpointManagement()
        {
            Dispose(false);
        }
    }
}