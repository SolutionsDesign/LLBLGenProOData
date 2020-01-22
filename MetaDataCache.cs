//////////////////////////////////////////////////////////////////////
// Part of the OData Support Classes for LLBLGen Pro. 
// LLBLGen Pro is (c) 2002-2020 Solutions Design. All rights reserved.
// http://www.llblgen.com
//////////////////////////////////////////////////////////////////////
// The OData Support Classes sourcecode is released under the following license:
// --------------------------------------------------------------------------------------------
// 
// The MIT License(MIT)
//   
// Copyright (c)2002-2020 Solutions Design. All rights reserved.
// https://www.llblgen.com
//   
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//   
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//   
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//////////////////////////////////////////////////////////////////////
// Contributers to the code:
//		- Brian Chance
//		- Frans Bouma [FB]
//////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data.Services.Providers;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>
	/// Simple cache which caches the meta-data provider for the service type specified. 
	/// </summary>
	internal static class MetaDataCache
	{
		private static Dictionary<Type, LLBLGenProODataServiceMetadataProvider> _cache = new Dictionary<Type, LLBLGenProODataServiceMetadataProvider>();
		private static ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		/// <summary>
		/// Initializes the <see cref="MetaDataCache"/> class.
		/// </summary>
		static MetaDataCache()
		{
		}


		/// <summary>
		/// Adds the instance specified, if there isn't an element with the key specified in the cache. If there is an element in the cache with the
		/// key specified, that element is returned. 
		/// </summary>
		/// <param name="serviceType">Type of the service.</param>
		/// <param name="metaDataProvider">The meta data provider.</param>
		/// <returns></returns>
		internal static LLBLGenProODataServiceMetadataProvider AddInstance(Type serviceType, LLBLGenProODataServiceMetadataProvider metaDataProvider)
		{
			LLBLGenProODataServiceMetadataProvider toReturn = null;
			_cacheLock.EnterWriteLock();
			try
			{
				if(!_cache.TryGetValue(serviceType, out toReturn))
				{
					_cache.Add(serviceType, metaDataProvider);
					toReturn = metaDataProvider;
				}
			}
			finally
			{
				_cacheLock.ExitWriteLock();
			}
			return toReturn;
		}


		/// <summary>
		/// Gets the instance cached under the key specified. 
		/// </summary>
		/// <param name="serviceType">Type of the service.</param>
		/// <returns></returns>
		internal static LLBLGenProODataServiceMetadataProvider GetInstance(Type serviceType)
		{
			_cacheLock.EnterReadLock();
			LLBLGenProODataServiceMetadataProvider toReturn = null;
			try
			{
				_cache.TryGetValue(serviceType, out toReturn);
			}
			finally
			{
				_cacheLock.ExitReadLock();
			}
			return toReturn;
		}
	}
}
