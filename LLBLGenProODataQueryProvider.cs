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
using System.Data.Services.Providers;
using System.Reflection;
using System.Globalization;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>
	/// The Query provider class for the WCF Data services support.
	/// </summary>
	/// <typeparam name="TLinqMetaData"></typeparam>
	public class LLBLGenProODataQueryProvider<TLinqMetaData> : IDataServiceQueryProvider
		where TLinqMetaData : class, new()
	{
		#region Class Member Declarations
		private LLBLGenProODataServiceMetadataProvider _metaDataProvider;
		private LLBLGenProODataServiceBase<TLinqMetaData> _containingService;
		private TLinqMetaData _currentDataSource;
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="LLBLGenProODataQueryProvider&lt;TLinqMetaData&gt;"/> class.
		/// </summary>
		/// <param name="metaDataProvider">The meta data provider.</param>
		/// <param name="containingService">The containing service.</param>
		public LLBLGenProODataQueryProvider(LLBLGenProODataServiceMetadataProvider metaDataProvider, 
						LLBLGenProODataServiceBase<TLinqMetaData> containingService)
		{
			if(metaDataProvider == null)
			{
				throw new ArgumentNullException(nameof(metaDataProvider));
			}
			if(containingService == null)
			{
				throw new ArgumentNullException(nameof(containingService));
			}
			_containingService = containingService;
			_metaDataProvider = metaDataProvider;
		}

		#region IDataServiceQueryProvider Members
		/// <summary>
		/// Gets the value of the open property.
		/// </summary>
		/// <param name="target">Instance of the type that declares the open property.</param>
		/// <param name="propertyName">Name of the open property.</param>
		/// <returns>The value of the open property.</returns>
		public object GetOpenPropertyValue(object target, string propertyName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the name and values of all the properties that are defined in the given instance of an open type.
		/// </summary>
		/// <param name="target">Instance of the type that declares the open property.</param>
		/// <returns>
		/// A collection of name and values of all the open properties.
		/// </returns>
		public IEnumerable<KeyValuePair<string, object>> GetOpenPropertyValues(object target)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the value of the open property.
		/// </summary>
		/// <param name="target">Instance of the type that declares the open property.</param>
		/// <param name="resourceProperty">Value for the open property.</param>
		/// <returns>Value for the property.</returns>
		public object GetPropertyValue(object target, ResourceProperty resourceProperty)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Invokes the given service operation and returns the results.
		/// </summary>
		/// <param name="serviceOperation">Service operation to invoke.</param>
		/// <param name="parameters">Values of parameters to pass to the service operation.</param>
		/// <returns>
		/// The result of the service operation, or a null value for a service operation that returns void.
		/// </returns>
		public object InvokeServiceOperation(ServiceOperation serviceOperation, object[] parameters)
		{
			return ((MethodInfo) serviceOperation.CustomState).Invoke(_containingService, 
																		BindingFlags.FlattenHierarchy | BindingFlags.Instance, null, parameters, 
																		CultureInfo.InvariantCulture);
		}


		/// <summary>
		/// Gets the <see cref="T:System.Linq.IQueryable`1"/> that represents the container.
		/// </summary>
		/// <param name="resourceSet">The resource set.</param>
		/// <returns>
		/// An <see cref="T:System.Linq.IQueryable`1"/> that represents the resource set, or a null value if there is no resource set for the specified <paramref name="resourceSet"/>.
		/// </returns>
		public IQueryable GetQueryRootForResourceSet(ResourceSet resourceSet)
		{
			if((resourceSet==null) || (_currentDataSource==null))
			{
				return null;
			}
			var customState = resourceSet.CustomState as ResourceSetCustomState;
			if(customState == null)
			{
				return null;
			}
			return customState.LinqMetaDataProperty.GetValue(_currentDataSource, null) as IQueryable;
		}


		/// <summary>
		/// Gets the resource type for the instance that is specified by the parameter.
		/// </summary>
		/// <param name="target">Instance to extract a resource type from.</param>
		/// <returns>
		/// The <see cref="T:System.Data.Services.Providers.ResourceType"/> of the supplied object.
		/// </returns>
		public ResourceType GetResourceType(object target)
		{
			if(target == null)
			{
				return null;
			}
			return _metaDataProvider.GetResourceTypeForEntityType(target.GetType());
		}


		/// <summary>
		/// The data source object from which data is provided.
		/// </summary>
		public object CurrentDataSource
		{
			get { return _currentDataSource; }
			set { _currentDataSource = value as TLinqMetaData; }
		}


		/// <summary>
		/// Gets a value that indicates whether null propagation is required in expression trees.
		/// </summary>
		public bool IsNullPropagationRequired
		{
			// no null propagation required, LLBLGen Pro can handle nulls in these scenarios
			get { return false; }
		}

		#endregion
	}
}
