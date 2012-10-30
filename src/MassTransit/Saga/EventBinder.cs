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
namespace MassTransit.Saga
{
    using System;
    using System.Linq.Expressions;

    public interface EventBinder<TSaga>
    {
        Expression<Func<TSaga, TMessage, bool>> GetBindExpression<TMessage>();
        Func<TMessage, Guid> GetCorrelationIdSelector<TMessage>();
    }

    public interface EventBinder<TSaga, TMessage> :
        EventBinder<TSaga>
    {
        EventBinder<TSaga, TMessage> By(Expression<Func<TSaga, TMessage, bool>> expression);
        EventBinder<TSaga, TMessage> UseId(Func<TMessage, Guid> correlationIdSelector);
    }
}