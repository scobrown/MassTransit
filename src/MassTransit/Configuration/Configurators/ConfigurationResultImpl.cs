﻿// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Configurators
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using Exceptions;
	using Util;

	[Serializable, DebuggerDisplay("{DebuggerString()}")]
	public class ConfigurationResultImpl :
		ConfigurationResult
	{
		readonly IList<ValidationResult> _results;

		ConfigurationResultImpl(IEnumerable<ValidationResult> results)
		{
			_results = results.ToList();
		}

		public IEnumerable<ValidationResult> Results
		{
			get { return _results; }
		}

	    public bool ContainsFailure
	    {
	        get { return _results.Any(x => x.Disposition == ValidationResultDisposition.Failure); }
	    }

		[UsedImplicitly]
		protected string DebuggerString()
		{
#if NET40
			var debuggerString = string.Join(", ", _results);
#else
			var debuggerString = string.Join(", ", _results.Select(x => x.ToString()).ToArray());
#endif

#if NET40
			return string.IsNullOrWhiteSpace(debuggerString)
#else
			return string.IsNullOrEmpty(debuggerString)
#endif
				? "No Obvious Problems says ConfigurationResult"
				: debuggerString;
		}

		public static ConfigurationResult CompileResults(IEnumerable<ValidationResult> results)
		{
			var result = new ConfigurationResultImpl(results);

			if (result.ContainsFailure)
			{
				string message = "The service bus was not properly configured:" +
				                 Environment.NewLine +
				                 string.Join(Environment.NewLine, result.Results.Select(x => x.ToString()).ToArray());

				throw new ConfigurationException(result, message);
			}

			return result;
		}
	}
}