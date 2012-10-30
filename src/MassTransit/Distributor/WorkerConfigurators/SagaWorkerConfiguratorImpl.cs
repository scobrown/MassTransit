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
namespace MassTransit.Distributor.WorkerConfigurators
{
    using System.Collections.Generic;
    using Builders;
    using Configurators;
    using Saga;
    using SubscriptionConfigurators;
    using WorkerConnectors;

    public class SagaWorkerConfiguratorImpl<TSaga> :
        SubscriptionConfiguratorImpl<SagaWorkerConfigurator<TSaga>>,
        SagaWorkerConfigurator<TSaga>,
        WorkerBuilderConfigurator
        where TSaga : class, ISaga
    {
        ISagaRepository<TSaga> _sagaRepository;

        public SagaWorkerConfiguratorImpl(ISagaRepository<TSaga> sagaRepository)
        {
            _sagaRepository = sagaRepository;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (_sagaRepository == null)
                yield return this.Failure("SagaRepository", "must not be null");
        }

        public void Configure(WorkerBuilder builder)
        {
            var configurator = new SagaWorkerConnector<TSaga>(ReferenceFactory, _sagaRepository);

            builder.Add(configurator);
        }
    }
}