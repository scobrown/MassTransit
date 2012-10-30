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
namespace MassTransit
{
    using Distributor.WorkerConfigurators;
    using Logging;
    using Magnum.Extensions;
    using Saga;

    public static class SagaWorkerConfiguratorExtensions
    {
        static readonly ILog _log = Logger.Get(typeof(SagaWorkerConfiguratorExtensions));

        public static SagaWorkerConfigurator<TSaga> Saga<TSaga>(
            this WorkerBusServiceConfigurator configurator, ISagaRepository<TSaga> sagaRepository)
            where TSaga : class, ISaga
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("Subscribing Saga Worker: {0}", typeof(TSaga).ToShortTypeName());

            var sagaConfigurator = new SagaWorkerConfiguratorImpl<TSaga>(sagaRepository);

            configurator.AddConfigurator(sagaConfigurator);

            return sagaConfigurator;
        }
    }
}