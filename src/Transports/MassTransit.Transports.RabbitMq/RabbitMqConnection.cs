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
namespace MassTransit.Transports.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Magnum.Extensions;
    using RabbitMQ.Client;

    public class RabbitMqConnection :
        Connection
    {
        static readonly ILog _log = Logger.Get(typeof (RabbitMqConnection));
        protected readonly ConnectionFactory ConnectionFactory;
        protected IConnection _connection;
        bool _disposed;

        public RabbitMqConnection(ConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory;
        }

        public IConnection Connection
        {
            get { return _connection; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Connect()
        {
            Disconnect();

            _connection = CreateConnection();
        }

        public void Disconnect()
        {
            try
            {
                if (_connection != null)
                {
                    try
                    {
                        if (_connection.IsOpen)
                            _connection.Close(200, "disconnected");
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Exception while closing RabbitMQ connection", ex);
                    }

                    _connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Exception disposing of RabbitMQ connection", ex);
            }
            finally
            {
                _connection = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_disposed)
                throw new ObjectDisposedException("RabbitMqConnection for {0}".FormatWith(ConnectionFactory.GetUri()),
                    "Cannot dispose a connection twice");

            try
            {
                Disconnect();
            }
            finally
            {
                _disposed = true;
            }
        }

        protected virtual IConnection CreateConnection()
        {
            _log.Info("Connecting to " + ConnectionFactory.HostName);
            return ConnectionFactory.CreateConnection();
        }
    }

    public interface RabbitHostConnectionPolicy
    {
        string GetServer();
    }

    public class RoundRobinConnectionPolicy : RabbitHostConnectionPolicy
    {
        readonly IEnumerable<string> _hostNames;
        IEnumerator<string> _hostEnumerator;

        public RoundRobinConnectionPolicy(IEnumerable<string> hostNames)
        {
            _hostNames = hostNames;
            _hostEnumerator = hostNames.GetEnumerator();
        }

        public string GetServer()
        {
            lock (_hostNames)
            {
                if (!_hostEnumerator.MoveNext())
                {
                    _hostEnumerator = _hostNames.GetEnumerator();
                    if (!_hostEnumerator.MoveNext())
                        throw new Exception("Enumeration did not yield a hostname.  No items in enumerable hostNames");
                }
                return _hostEnumerator.Current;
            }
        }
    }
    public class PolicyBasedRabbitMqConnection : RabbitMqConnection
    {
        readonly RabbitHostConnectionPolicy _connectionPolicy;

        public PolicyBasedRabbitMqConnection(ConnectionFactory connectionFactory, RabbitHostConnectionPolicy connectionPolicy) :
            base(connectionFactory)
        {
            _connectionPolicy = connectionPolicy;
        }


        protected override IConnection CreateConnection()
        {
            ConnectionFactory.HostName = _connectionPolicy.GetServer();

            return base.CreateConnection();
        }
    }
}