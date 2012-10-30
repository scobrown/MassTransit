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
    using System.Collections.Generic;
    using Configuration;
    using Context;
    using Magnum.Extensions;
    using Magnum.StateMachine;

    public static class ExtensionsToStateMachine
    {
        public static DataEventAction<T, TData> Publish<T, TData, TMessage>(this DataEventAction<T, TData> eventAction,
            Func<T, TData, TMessage> action)
            where T : SagaStateMachine<T>, ISaga
            where TData : class
            where TMessage : class
        {
            eventAction.Call((saga, message) => saga.Bus.Publish(action(saga, message)));
            return eventAction;
        }

        public static BasicEventAction<T> Publish<T, TMessage>(this BasicEventAction<T> eventAction,
            Func<T, TMessage> action)
            where T : SagaStateMachine<T>, ISaga
            where TMessage : class
        {
            Action<T> f = saga => saga.Bus.Publish(action(saga));
            eventAction.Call(saga => f(saga));
            return eventAction;
        }

        public static DataEventAction<T, TData> RespondWith<T, TData, TMessage>(
            this DataEventAction<T, TData> eventAction, Func<T, TData, TMessage> action)
            where T : SagaStateMachine<T>, ISaga
            where TData : class
            where TMessage : class
        {
            eventAction.Call((saga, message) => ContextStorage.Context().Respond(action(saga, message)));
            return eventAction;
        }

        public static DataEventAction<T, TData> RetryLater<T, TData>(this DataEventAction<T, TData> eventAction)
            where T : SagaStateMachine<T>, ISaga
            where TData : class
        {
            eventAction.Call((saga, message) => ContextStorage.MessageContext<TData>().RetryLater());
            return eventAction;
        }

        public static void EnumerateDataEvents<T>(this T saga, Action<Type> messageAction)
            where T : SagaStateMachine<T>, ISaga
        {
            var inspector = new SagaStateMachineEventInspector<T>();
            saga.Inspect(inspector);

            inspector.GetResults().Each(x => { messageAction(x.SagaEvent.MessageType); });
        }

        public static void EnumerateDataEvents<T>(this T saga, Action<SagaEvent<T>, IEnumerable<State>> messageAction)
            where T : SagaStateMachine<T>, ISaga
        {
            var inspector = new SagaStateMachineEventInspector<T>();
            saga.Inspect(inspector);

            inspector.GetResults().Each(x => { messageAction(x.SagaEvent, x.States); });
        }
    }
}