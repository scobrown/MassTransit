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
namespace MassTransit.Distributor.DistributorConfigurators
{
    using System.Collections.Generic;
    using Builders;
    using Configurators;
    using DistributorConnectors;
    using Saga;

    public class SagaDistributorConfiguratorImpl<TSaga> :
        DistributorConfiguratorImpl<SagaDistributorConfigurator<TSaga>>,
        SagaDistributorConfigurator<TSaga>,
        DistributorBuilderConfigurator
        where TSaga : class, ISaga
    {
        readonly ISagaRepository<TSaga> _sagaRepository;

        public SagaDistributorConfiguratorImpl(ISagaRepository<TSaga> sagaRepository)
        {
            _sagaRepository = sagaRepository;
        }

        public override IEnumerable<ValidationResult> Validate()
        {
            foreach (ValidationResult result in base.Validate())
            {
                yield return result;
            }

            if (_sagaRepository == null)
                yield return this.Failure("SagaRepository", "must not be null");
        }

        public void Configure(DistributorBuilder builder)
        {
            var connector = new SagaDistributorConnector<TSaga>(ReferenceFactory, WorkerSelectorFactory, _sagaRepository);

            builder.Add(connector);
        }
    }
}