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
namespace MassTransit.SubscriptionConfigurators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Configurators;
    using Magnum.Extensions;
    using SubscriptionConnectors;

    public static class ConsumerFactoryConfiguratorExtensions
    {
        public static IEnumerable<ValidationResult> ValidateConsumer<TConsumer>(this Configurator configurator)
            where TConsumer : class
        {
            if (!typeof(TConsumer).Implements<IConsumer>())
                yield return configurator.Warning("Consumer",
                    string.Format("The consumer class {0} does not implement any IConsumer interfaces",
                        typeof(TConsumer).ToShortTypeName()));

            IEnumerable<ValidationResult> warningForMessages = MessageInterfaceTypeReflector<TConsumer>
                .GetAllTypes()
                .Distinct()
                .Where(x => !(HasDefaultProtectedCtor(typeof(TConsumer)) || HasSinglePublicCtor(typeof(TConsumer))))
                .Select(x => ("The {0} consumer should have a public or protected default constructor." +
                              " Without an available constructor, MassTransit will initialize new consumer instances" +
                              " without calling a constructor, which can lead to unpredictable behavior if the consumer" +
                              " depends upon logic in the constructor to be executed.")
                                 .FormatWith(x.MessageType.ToShortTypeName()))
                .Select(message => configurator.Warning("Consumer", message));

            foreach (ValidationResultImpl message in warningForMessages)
                yield return message;
        }

        public static IEnumerable<ValidationResult> Validate<TConsumer>(this IConsumerFactory<TConsumer> consumerFactory)
            where TConsumer : class
        {
            if (consumerFactory == null)
                yield return ValidationResultExtensions.Failure(null, "ConsumerFactory", "must not be null");

            foreach (ValidationResult result in ValidateConsumer<TConsumer>(null))
            {
                yield return result;
            }
        }

        static bool HasDefaultProtectedCtor(Type type)
        {
            return
                type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Any(
                    c => !c.GetParameters().Any());
        }

        static bool HasSinglePublicCtor(Type type)
        {
            return
                type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).All(
                    c => !c.GetParameters().Any())
                && type.GetConstructors().Count() == 1;
        }
    }
}