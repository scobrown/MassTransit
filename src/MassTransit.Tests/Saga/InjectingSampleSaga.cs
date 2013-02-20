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
namespace MassTransit.Tests.Saga
{
    using System;
    using MassTransit.Saga;


    public class InjectingSampleSaga :
        InitiatedBy<InitiateSimpleSaga>,
        ISaga
    {
        public InjectingSampleSaga()
        {
        }

        public InjectingSampleSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public string Name { get; private set; }
        public IDependency Dependency { get; set; }
        public void Consume(InitiateSimpleSaga message)
        {
            Name = message.Name;
        }

        public Guid CorrelationId { get; set; }

        public IServiceBus Bus { get; set; }
    }


    public interface IDependency
    {
    }
}