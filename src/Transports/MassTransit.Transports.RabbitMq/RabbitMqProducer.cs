// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Transports.RabbitMq
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using RabbitMQ.Client;


    public class RabbitMqProducer :
        ConnectionBinding<RabbitMqConnection>
    {
        readonly IRabbitMqEndpointAddress _address;
        readonly bool _bindToQueue;
        readonly object _channelLock = new object();
        readonly HashSet<ExchangeBinding> _exchangeBindings;
        readonly HashSet<string> _exchanges;
        readonly Dictionary<int, IModel> _threadChannels = new Dictionary<int, IModel>();
        IModel Channel
        {
            get { return _threadChannels[Thread.CurrentThread.ManagedThreadId]; }
        }


        public RabbitMqProducer(IRabbitMqEndpointAddress address, bool bindToQueue)
        {
            _address = address;
            _bindToQueue = bindToQueue;
            _exchangeBindings = new HashSet<ExchangeBinding>();
            _exchanges = new HashSet<string>();
        }

        public void Bind(RabbitMqConnection connection)
        {
            _threadChannels[Thread.CurrentThread.ManagedThreadId] = null;
            lock (_channelLock)
                foreach (var threadChannel in _threadChannels.Keys.ToArray())
                {
                    _threadChannels[threadChannel] = connection.Connection.CreateModel();
                }

            DeclareAndBindQueue();

            RebindExchanges();
        }

        public void Unbind(RabbitMqConnection connection)
        {
            lock (_channelLock)
            {
                foreach (var threadChannel in _threadChannels)
                {
                    if (threadChannel.Value.IsOpen)
                        threadChannel.Value.Close(200, "producer unbind");
                    threadChannel.Value.Dispose();
                    _threadChannels[threadChannel.Key] = null;
                }
            }
        }

        public void ExchangeDeclare(string name)
        {
            lock (_exchangeBindings)
                _exchanges.Add(name);
        }

        public void ExchangeBind(string destination, string source)
        {
            var binding = new ExchangeBinding(destination, source);

            lock (_exchangeBindings)
                _exchangeBindings.Add(binding);
        }

        void DeclareAndBindQueue()
        {
            Channel.ExchangeDeclare(_address.Name, ExchangeType.Fanout, true);

            if (_bindToQueue)
            {
                string queue = Channel.QueueDeclare(_address.Name, true, false, false, _address.QueueArguments());

                Channel.QueueBind(queue, _address.Name, "");
            }
        }

        void RebindExchanges()
        {
            lock (_exchangeBindings)
            {
                IEnumerable<string> exchanges = _exchangeBindings.Select(x => x.Destination)
                                                                 .Concat(_exchangeBindings.Select(x => x.Source))
                                                                 .Concat(_exchanges)
                                                                 .Distinct();

                foreach (string exchange in exchanges)
                {
                    Channel.ExchangeDeclare(exchange, ExchangeType.Fanout, true, false, null);
                }

                foreach (ExchangeBinding exchange in _exchangeBindings)
                {
                    Channel.ExchangeBind(exchange.Destination, exchange.Source, "");
                }
            }
        }

        public IBasicProperties CreateProperties()
        {
            if (Channel == null)
                throw new InvalidConnectionException(_address.Uri, "Channel should not be null");

            return Channel.CreateBasicProperties();
        }

        public void Publish(string exchangeName, IBasicProperties properties, byte[] body)
        {
            if (Channel == null)
                throw new InvalidConnectionException(_address.Uri, "Channel should not be null");

            Channel.BasicPublish(exchangeName, "", properties, body);
        }
    }
}