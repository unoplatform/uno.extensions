// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Core.Tests.TestUtils
{
	public class AsyncTestContext
	{
		private static long _nextId;
		private static readonly AsyncLocal<AsyncTestContext> _context = new AsyncLocal<AsyncTestContext>();

		public long Id { get; } = Interlocked.Increment(ref _nextId);

		public static AsyncTestContext Current
		{
			get
			{
				var context = _context.Value;
				if (context == null)
				{
					_context.Value = context = new AsyncTestContext();
				}

				return context;
			}
		}

		private AsyncTestContext()
		{
		}

		public void Validate()
		{
			Assert.AreEqual(this, _context.Value);
		}
	}
}
