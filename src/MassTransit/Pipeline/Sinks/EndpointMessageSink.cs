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
namespace MassTransit.Pipeline.Sinks
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// A message sink that sends to an endpoint
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	public class EndpointMessageSink<TMessage> :
		OutboundPipelineSink<TMessage>
		where TMessage : class
	{
		readonly IEndpoint _endpoint;

		public EndpointMessageSink(IEndpoint endpoint)
		{
			_endpoint = endpoint;
		}

		public IEndpoint Endpoint
		{
			get { return _endpoint; }
		}

		public IEnumerable<Action<IBusPublishContext<TMessage>>> Enumerate(IBusPublishContext<TMessage> context)
		{
			yield return x =>
				{
					if (x.WasEndpointAlreadySent(_endpoint.Address))
						return;

					_endpoint.Send((ISendContext<TMessage>)x);
				};
		}

		public bool Inspect(IPipelineInspector inspector)
		{
			inspector.Inspect(this);
			return true;
		}
	}
}