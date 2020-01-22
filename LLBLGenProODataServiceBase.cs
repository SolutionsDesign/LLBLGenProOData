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
using System.Data.Services;
using System.Data.Services.Providers;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>
	/// Base class implementation for WCF Data Services dataservice classes using LLBLGen Pro. 
	/// </summary>
	/// <typeparam name="TLinqMetaData">The type of the LinqMetaData class to use by the service</typeparam>
	public abstract class LLBLGenProODataServiceBase<TLinqMetaData> : DataService<TLinqMetaData>, IServiceProvider
		where TLinqMetaData : class, new()
	{
		#region Class Member Declarations
		private LLBLGenProODataServiceMetadataProvider _modelMetadata;
		private LLBLGenProODataQueryProvider<TLinqMetaData> _queryProvider;
		private LLBLGenProODataServiceExpandProvider _expandProvider;
		private LLBLGenProODataServiceUpdateProvider _updateProvider;
		#endregion

		/// <summary>
		/// Creates a new TLinqMetaData instance. 
		/// </summary>
		/// <returns>A ready to use TLinqMetaData instance</returns>
		protected abstract TLinqMetaData CreateLinqMetaDataInstance();
		/// <summary>
		/// Creates a ready to use transaction controller. For Adapter, create a new DataAccessAdapter instance. For Selfservicing, create a new 
		/// Transaction instance.
		/// </summary>
		/// <returns>ready to use transaction controller.</returns>
		protected abstract ITransactionController CreateTransactionControllerInstance();
		/// <summary>
		/// Creates a ready to use unit of work instance. For Adapter, create a new UnitOfWork2 instance. For SelfServicing, create a new
		/// UnitOfWork instance. 
		/// </summary>
		/// <returns>ready to use unit of work instance</returns>
		protected abstract IUnitOfWorkCore CreateUnitOfWorkInstance();


		/// <summary>
		/// Gets the model meta data.
		/// </summary>
		/// <returns>ready to use ModelMetaData class</returns>
		private LLBLGenProODataServiceMetadataProvider GetModelMetadata()
		{
			if(_modelMetadata == null)
			{
				_modelMetadata = MetaDataCache.GetInstance(this.GetType());
				if(_modelMetadata == null)
				{
					// not yet in cache. Create new one.
					var newModelMetadata = new LLBLGenProODataServiceMetadataProvider(this.GetType(), typeof(TLinqMetaData), this.ContainerName, 
																					  this.ContainerNamespace, this.AllowSubTypeNavigators);
					_modelMetadata = MetaDataCache.AddInstance(this.GetType(), newModelMetadata);
				}
			}
			return _modelMetadata;
		}


		/// <summary>
		/// Gets the query provider.
		/// </summary>
		/// <returns></returns>
		private IDataServiceQueryProvider GetQueryProvider()
		{
			if(_queryProvider == null)
			{
				_queryProvider = new LLBLGenProODataQueryProvider<TLinqMetaData>(this.Metadata, this) { CurrentDataSource = CreateLinqMetaDataInstance() };
			}
			return _queryProvider;
		}


		/// <summary>
		/// Gets the expand provider.
		/// </summary>
		/// <returns></returns>
		private LLBLGenProODataServiceExpandProvider GetExpandProvider()
		{
			if(_expandProvider == null)
			{
				_expandProvider = new LLBLGenProODataServiceExpandProvider();
			}
			return _expandProvider;
		}


		/// <summary>
		/// Gets the update provider.
		/// </summary>
		/// <returns></returns>
		private LLBLGenProODataServiceUpdateProvider GetUpdateProvider()
		{
			if(_updateProvider == null)
			{
				_updateProvider = new LLBLGenProODataServiceUpdateProvider(CreateUnitOfWorkInstance(), () => CreateTransactionControllerInstance(), this.Metadata);
			}
			return _updateProvider;
		}
		

		#region IServiceProvider Members
		/// <summary>Returns service implementation.</summary>
		/// <param name="serviceType">The type of the service requested.</param>
		/// <returns>Implementation of such service or null.</returns>
		public object GetService(Type serviceType)
		{
			if(serviceType == typeof(IDataServiceMetadataProvider))
			{
				return this.Metadata;
			}
			else if(serviceType == typeof(IDataServiceQueryProvider))
			{
				return this.QueryProvider;
			}
			// [FB] Commented out since June 2nd, 2013, because IExpandProvider is deprecated and it will cause WCF Data Services return
			//      less data. Not returning an IExpandProvider implementation will make the WCF Data Services construct nested queries, which are
			//      handled the same way as prefetch paths but will return the right set of data.
			// 
			// else if(serviceType == typeof(IExpandProvider))
			// {
			// 	 return this.ExpandProvider;
			// }
			else if(serviceType == typeof(IDataServiceUpdateProvider))
			{
				return this.UpdateProvider;
			}
			else
			{
				return null;
			}
		}
		#endregion
		
		#region Class Property Declarations
		/// <summary>
		/// Gets the query provider.
		/// </summary>
		protected virtual IDataServiceQueryProvider QueryProvider
		{
			get { return GetQueryProvider(); }
		}
		
		/// <summary>
		/// Gets the meta data of the model represented by the service
		/// </summary>
		protected virtual LLBLGenProODataServiceMetadataProvider Metadata
		{
			get { return GetModelMetadata(); }
		}
		
		/// <summary>
		/// Gets the expand provider.
		/// </summary>
		protected virtual LLBLGenProODataServiceExpandProvider ExpandProvider
		{
			get { return GetExpandProvider(); }
		}

		/// <summary>
		/// Gets the update provider.
		/// </summary>
		protected virtual LLBLGenProODataServiceUpdateProvider UpdateProvider
		{
			get { return GetUpdateProvider(); }
		}
		
		/// <summary>
		/// Gets the name of the container. This value is used for example when a proxy is generated by VS through Add Service Reference.
		/// The main context class generated will have the ContainerName. By default it returns "LLBLGenProODataService"
		/// </summary>
		protected virtual string ContainerName
		{
			get { return "LLBLGenProODataService"; }
		}

		/// <summary>
		/// Gets the container namespace. This is used in the $metadata response. By default it returns the namespace of the TLinqMetaData type.
		/// </summary>
		protected virtual string ContainerNamespace
		{
			get { return typeof(TLinqMetaData).Namespace; }
		}

		/// <summary>
		/// Gets a value indicating whether subtype navigators are allowed (true, default) or not. As ODataSupportClasses is compiled against
		/// an assembly which supports OData v3, the navigators of subtypes are by default allowed (OData v3 or higher). If you're communicating
		/// with clients on OData v2, override this property and return false. 
		/// </summary>
		protected virtual bool AllowSubTypeNavigators
		{
			get { return true; }
		}
		#endregion
	}
}
