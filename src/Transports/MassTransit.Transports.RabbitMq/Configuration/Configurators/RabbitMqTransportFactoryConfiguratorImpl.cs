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
namespace MassTransit.Transports.RabbitMq.Configuration.Configurators
{
    using System;
    using System.Collections.Generic;
	using System.Linq;
	using Builders;
	using MassTransit.Configurators;
    using RabbitMQ.Client;
    using Util;

    public class RabbitMqTransportFactoryConfiguratorImpl :
		RabbitMqTransportFactoryConfigurator
	{
		readonly IList<RabbitMqTransportFactoryBuilderConfigurator> _transportFactoryConfigurators;
	    Func<ConnectionFactory, RabbitMqConnection> _connectionInitializer =
            x=>new RabbitMqConnection(x);

	    public RabbitMqTransportFactoryConfiguratorImpl()
		{
			_transportFactoryConfigurators = new List<RabbitMqTransportFactoryBuilderConfigurator>();
		}

		public IEnumerable<ValidationResult> Validate()
		{
			return _transportFactoryConfigurators.SelectMany(x => x.Validate());
		}

		public void AddConfigurator(RabbitMqTransportFactoryBuilderConfigurator configurator)
		{
			_transportFactoryConfigurators.Add(configurator);
		}

        public void UseRoundRobinConnectionPolicy(IEnumerable<string> hosts)
        {
            var policy = new RoundRobinConnectionPolicy(hosts);//Declare here to make it singleton
	        _connectionInitializer = x => new PolicyBasedRabbitMqConnection(x, policy);
	    }
        public void UseCustomConnectionPolicy(RabbitHostConnectionPolicy policy)
        {
            _connectionInitializer = x=> new PolicyBasedRabbitMqConnection(x, policy);
        }

	    public RabbitMqTransportFactory Build()
		{
			var builder = new RabbitMqTransportFactoryBuilderImpl(_connectionInitializer);

			_transportFactoryConfigurators.Aggregate((RabbitMqTransportFactoryBuilder) builder,
				(seed, configurator) => configurator.Configure(seed));

			return builder.Build();
		}
	}
}