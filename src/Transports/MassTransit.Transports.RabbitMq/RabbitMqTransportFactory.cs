﻿// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Configuration.Builders;
    using Configuration.Configurators;
    using Exceptions;
    using Magnum.Extensions;
    using Magnum.Threading;
    using RabbitMQ.Client;

    public class RabbitMqTransportFactory :
        ITransportFactory
    {
        readonly ReaderWriterLockedDictionary<Uri, ConnectionHandler<RabbitMqConnection>> _connectionCache;
        readonly IDictionary<Uri, ConnectionFactoryBuilder> _connectionFactoryBuilders;
        readonly IMessageNameFormatter _messageNameFormatter;
        bool _disposed;

        public RabbitMqTransportFactory(IDictionary<Uri, ConnectionFactoryBuilder> connectionFactoryBuilders)
        {
            _connectionCache = new ReaderWriterLockedDictionary<Uri, ConnectionHandler<RabbitMqConnection>>();
            _connectionFactoryBuilders = connectionFactoryBuilders;
            _messageNameFormatter = new RabbitMqMessageNameFormatter();
        }

        public RabbitMqTransportFactory()
        {
            _connectionCache = new ReaderWriterLockedDictionary<Uri, ConnectionHandler<RabbitMqConnection>>();
            _connectionFactoryBuilders = new Dictionary<Uri, ConnectionFactoryBuilder>();
            _messageNameFormatter = new RabbitMqMessageNameFormatter();
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
				_connectionCache.Values.Each(x => x.Dispose());
				_connectionCache.Clear();

				_connectionCache.Dispose();
			}

			_disposed = true;
		}

        public string Scheme
        {
            get { return "rabbitmq"; }
        }

        public IDuplexTransport BuildLoopback(ITransportSettings settings)
        {
            RabbitMqEndpointAddress address = RabbitMqEndpointAddress.Parse(settings.Address.Uri);

            var transport = new Transport(address, () => BuildInbound(settings), () => BuildOutbound(settings));

            return transport;
        }

        public IInboundTransport BuildInbound(ITransportSettings settings)
        {
            RabbitMqEndpointAddress address = RabbitMqEndpointAddress.Parse(settings.Address.Uri);

            EnsureProtocolIsCorrect(address.Uri);

            ConnectionHandler<RabbitMqConnection> connectionHandler = GetConnection(address);

            return new InboundRabbitMqTransport(address, connectionHandler, settings.PurgeExistingMessages,
                _messageNameFormatter);
        }

        public IOutboundTransport BuildOutbound(ITransportSettings settings)
        {
            RabbitMqEndpointAddress address = RabbitMqEndpointAddress.Parse(settings.Address.Uri);

            EnsureProtocolIsCorrect(address.Uri);

            ConnectionHandler<RabbitMqConnection> connectionHandler = GetConnection(address);

            return new OutboundRabbitMqTransport(address, connectionHandler, false);
        }

        public IOutboundTransport BuildError(ITransportSettings settings)
        {
            RabbitMqEndpointAddress address = RabbitMqEndpointAddress.Parse(settings.Address.Uri);

            EnsureProtocolIsCorrect(address.Uri);

            ConnectionHandler<RabbitMqConnection> connection = GetConnection(address);

            return new OutboundRabbitMqTransport(address, connection, true);
        }

        public IMessageNameFormatter MessageNameFormatter
        {
            get { return _messageNameFormatter; }
        }

        public int ConnectionCount()
        {
            return _connectionCache.Count();
        }

        ConnectionHandler<RabbitMqConnection> GetConnection(IRabbitMqEndpointAddress address)
        {
            return _connectionCache.Retrieve(address.Uri, () =>
                {
                    ConnectionFactoryBuilder builder = _connectionFactoryBuilders.Retrieve(address.Uri, () =>
                        {
                            var configurator = new ConnectionFactoryConfiguratorImpl(address);

                            return configurator.CreateBuilder();
                        });

                    builder.Build();

                    var connection = new RabbitMqConnection(address);
                    var connectionHandler = new ConnectionHandlerImpl<RabbitMqConnection>(connection);
                    return connectionHandler;
                });
        }

        static void EnsureProtocolIsCorrect(Uri address)
        {
            if (address.Scheme != "rabbitmq")
                throw new EndpointException(address,
                    "Address must start with 'rabbitmq' not '{0}'".FormatWith(address.Scheme));
        }
    }

    internal class ClusterFailoverPolicy : ConnectionPolicy
    {
        private readonly ConnectionHandlerImpl<RabbitMqConnection> _connectionHandler;
        private readonly ConnectionPolicyChain _policyChain;
        private readonly TimeSpan _reconnectDelay;
        private readonly ConnectionFactory _connectionFactory;
        private readonly IRabbitMqEndpointAddress _address;

        public ClusterFailoverPolicy(ConnectionHandlerImpl<RabbitMqConnection> connectionHandler, 
            ConnectionPolicyChain policyChain,
            TimeSpan reconnectDelay,
            ConnectionFactory connectionFactory, 
            IRabbitMqEndpointAddress address)
        {
            _connectionHandler = connectionHandler;
            _policyChain = policyChain;
            _reconnectDelay = reconnectDelay;
            _connectionFactory = connectionFactory;
            _address = address;
        }

        public void Execute(Action callback)
        {
            _connectionHandler.Disconnect();

            if (_reconnectDelay > TimeSpan.Zero)
                Thread.Sleep(_reconnectDelay);

            _connectionFactory.HostName = _address.NextServerInCluster();
            _connectionHandler.Connect();

            _policyChain.Pop(this);
            _policyChain.Next(callback);
        }
    }
}