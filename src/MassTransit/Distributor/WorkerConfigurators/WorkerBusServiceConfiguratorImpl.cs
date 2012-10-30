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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Builders;
    using BusConfigurators;
    using BusServiceConfigurators;
    using Configurators;
    using MassTransit.Builders;

    public class WorkerBusServiceConfiguratorImpl :
        WorkerBusServiceConfigurator,
        BusServiceConfigurator,
        BusBuilderConfigurator
    {
        readonly IList<WorkerBuilderConfigurator> _configurators;

        public WorkerBusServiceConfiguratorImpl()
        {
            _configurators = new List<WorkerBuilderConfigurator>();
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return _configurators.SelectMany(configurator => configurator.Validate(),
                (configurator, result) => result.WithParentKey("Worker"));
        }

        public BusBuilder Configure(BusBuilder builder)
        {
            builder.AddBusServiceConfigurator(this);

            return builder;
        }

        public Type ServiceType
        {
            get { return typeof(WorkerBusService); }
        }

        public BusServiceLayer Layer
        {
            get { return BusServiceLayer.Presentation; }
        }

        public IBusService Create(IServiceBus bus)
        {
            var builder = new WorkerBuilderImpl();

            foreach (WorkerBuilderConfigurator configurator in _configurators)
            {
                configurator.Configure(builder);
            }

            return builder.Build();
        }

        public void AddConfigurator(WorkerBuilderConfigurator configurator)
        {
            _configurators.Add(configurator);
        }
    }
}