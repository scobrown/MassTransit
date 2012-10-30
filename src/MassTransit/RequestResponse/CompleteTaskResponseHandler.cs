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
namespace MassTransit.RequestResponse
{
#if NET40
    using System;

    public class CompleteTaskResponseHandler<TResponse> :
        TaskResponseHandlerBase<TResponse>
        where TResponse : class
    {
        public CompleteTaskResponseHandler(string requestId, Action<IConsumeContext<TResponse>, TResponse> handler)
            : base(requestId, handler)
        {
        }

        public CompleteTaskResponseHandler(string requestId, Action<TResponse> handler)
            : base(requestId, handler)
        {
        }

        protected override void Success(IConsumeContext<TResponse> context)
        {
            CompletionSource.SetResult(context.Message);
        }

        protected override void Failure(Exception exception)
        {
            CompletionSource.TrySetException(exception);
        }
    }

#endif
}