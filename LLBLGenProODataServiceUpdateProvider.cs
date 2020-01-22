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
using SD.LLBLGen.Pro.ORMSupportClasses;
using System.Collections;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>
	/// Class which provides CUD support (insert/update/delete for WCF Data Services.
	/// </summary>
	/// <remarks>Code based on original templates made by Brian Chance</remarks>
	public class LLBLGenProODataServiceUpdateProvider : IDataServiceUpdateProvider
	{
		#region Class Member Declarations
		private IUnitOfWorkCore _uow;
		private Func<ITransactionController> _transactionControllerCreatorFunc;
		private LLBLGenProODataServiceMetadataProvider _metaDataProvider;
		#endregion


		/// <summary>
		/// Initializes a new instance of the <see cref="LLBLGenProODataServiceUpdateProvider"/> class.
		/// </summary>
		/// <param name="unitOfWork">The unit of work.</param>
		/// <param name="transactionControllerCreatorFunc">The transaction controller creator func.</param>
		/// <param name="metaDataProvider">The meta data provider.</param>
		public LLBLGenProODataServiceUpdateProvider(IUnitOfWorkCore unitOfWork, Func<ITransactionController> transactionControllerCreatorFunc, 
													LLBLGenProODataServiceMetadataProvider metaDataProvider)
		{
			if(unitOfWork == null)
			{
				throw new ArgumentNullException(nameof(unitOfWork));
			}
			if(transactionControllerCreatorFunc == null)
			{
				throw new ArgumentNullException(nameof(transactionControllerCreatorFunc));
			}
			if(metaDataProvider == null)
			{
				throw new ArgumentNullException(nameof(metaDataProvider));
			}
			_uow = unitOfWork;
			_transactionControllerCreatorFunc = transactionControllerCreatorFunc;
			_metaDataProvider = metaDataProvider;
			// make sure the entity is marked as saved after a save, as re-fetching is not needed. 
			EntityBase.MarkSavedEntitiesAsFetched = true;
			EntityBase2.MarkSavedEntitiesAsFetched = true;
		}


		/// <summary>
		/// Gets the resource of the specified type identified by a query and type name.
		/// </summary>
		/// <param name="query">Language integratee query(LINQ) pointing to a particular resource.</param>
		/// <param name="fullTypeName">The fully qualified type name of resource.</param>
		/// <returns>
		/// An opaque object representing a resource of the specified type, referenced by the specified query.
		/// </returns>
		public object GetResource(IQueryable query, string fullTypeName)
		{
			object resource = null;

			// enumerate to execute the query. We are only interested in the first element. FirstOrDefault() would be useful here but that requires a
			// typed generic IQueryable, and the interface is not generically typed. 
			bool first = true;
			foreach(object o in query)
			{
				if(!first)
				{
					break;
				}
				resource = o;
				first = false;
			}

			if((resource != null) && !string.IsNullOrEmpty(fullTypeName))
			{
				var entityType = _metaDataProvider.GetRealEntityTypeFromServiceEntityTypeName(fullTypeName);
				if(entityType==null)
				{
					throw new InvalidOperationException(string.Format("Unknown entity type '{0}'", fullTypeName));
				}
				if(!entityType.IsAssignableFrom(resource.GetType()))
				{
					throw new InvalidOperationException(string.Format("Resource read is of type '{0}', however type expected was '{1}'",
							resource.GetType().FullName, entityType.FullName));
				}
			}

			_uow.AddForSave(resource as IEntityCore);
			return resource;
		}


		/// <summary>
		/// Creates the resource of the specified type and that belongs to the specified container.
		/// </summary>
		/// <param name="containerName">The name of the entity set to which the resource belongs.</param>
		/// <param name="fullTypeName">The full namespace-qualified type name of the resource.</param>
		/// <returns>
		/// The object representing a resource of specified type and belonging to the specified container.
		/// </returns>
		public object CreateResource(string containerName, string fullTypeName)
		{
			Type t = _metaDataProvider.GetRealEntityTypeFromServiceEntityTypeName(fullTypeName);
			// use factory as abstract types don't have a public ctor
			var factory = _metaDataProvider.GetCachedFactoryForEntityType(t);
			IEntityCore resource=null;
			if(factory != null)
			{
				resource = factory.Create();
				_uow.AddForSave(resource);
			}
			return resource;
		}


		/// <summary>
		/// Updates the resource identified by the parameter <paramref name="resource"/>.
		/// </summary>
		/// <param name="resource">The resource to be updated.</param>
		/// <returns></returns>
		public object ResetResource(object resource)
		{
			var resourceAsEntity = resource as IEntityCore;
			if(resourceAsEntity != null)
			{
				resourceAsEntity.RejectChanges();
			}
			return resource;
		}


		/// <summary>
		/// Gets the value of the specified property on the target object.
		/// </summary>
		/// <param name="targetResource">An opaque object that represents a resource.</param>
		/// <param name="propertyName">The name of the property whose value needs to be retrieved.</param>
		/// <returns></returns>
		public object GetValue(object targetResource, string propertyName)
		{
			object toReturn = null;
			var property = _metaDataProvider.GetCachedPropertyForEntityType(targetResource.GetType(), propertyName);
			if(property != null)
			{
				toReturn = property.GetValue(targetResource);
			}
			return toReturn;
		}


		/// <summary>
		/// Sets the value of the property with the specified name on the target resource to the specified property value.
		/// </summary>
		/// <param name="targetResource">The target object that defines the property.</param>
		/// <param name="propertyName">The name of the property whose value needs to be updated.</param>
		/// <param name="propertyValue">The property value for update.</param>
		public void SetValue(object targetResource, string propertyName, object propertyValue)
		{
			// for some reason MS tries to set the primary key fields on insert. This could cause an error with identity fields, so
			// only set the property if the property isn't a readonly field.
			var resourceAsEntity = targetResource as IEntityCore;
			if(resourceAsEntity != null)
			{
				var field = resourceAsEntity.GetFieldByName(propertyName);
				if(field != null)
				{
					if(field.IsReadOnly)
					{
						// no can do. Return silently. 
						return;
					}
				}
			}
			var property = _metaDataProvider.GetCachedPropertyForEntityType(targetResource.GetType(), propertyName);
			if(property != null)
			{
				property.SetValue(targetResource, propertyValue);
			}
		}


		/// <summary>
		/// Sets the value of the specified reference property on the target object.
		/// </summary>
		/// <param name="targetResource">The target object that defines the property.</param>
		/// <param name="propertyName">The name of the property whose value needs to be updated.</param>
		/// <param name="propertyValue">The property value to be updated.</param>
		public void SetReference(object targetResource, string propertyName, object propertyValue)
		{
			// this reference can also be a custom property, so use a reflected property info
			this.SetValue(targetResource, propertyName, propertyValue);
		}


		/// <summary>
		/// Adds the specified value to the collection.
		/// </summary>
		/// <param name="targetResource">Target object that defines the property.</param>
		/// <param name="propertyName">The name of the collection property to which the resource should be added..</param>
		/// <param name="resourceToBeAdded">The opaque object representing the resource to be added.</param>
		public void AddReferenceToCollection(object targetResource, string propertyName, object resourceToBeAdded)
		{
			IList collection = GetEntityCollectionForNavigator(targetResource as IEntityCore, propertyName);
			if(collection != null)
			{
				collection.Add(resourceToBeAdded);
			}
		}


		/// <summary>
		/// Removes the specified value from the collection.
		/// </summary>
		/// <param name="targetResource">The target object that defines the property.</param>
		/// <param name="propertyName">The name of the property whose value needs to be updated.</param>
		/// <param name="resourceToBeRemoved">The property value that needs to be removed.</param>
		public void RemoveReferenceFromCollection(object targetResource, string propertyName, object resourceToBeRemoved)
		{
			IList collection = GetEntityCollectionForNavigator(targetResource as IEntityCore, propertyName);
			if(collection != null)
			{
				collection.Remove(resourceToBeRemoved);
			}
		}
		

		/// <summary>
		/// Deletes the specified resource.
		/// </summary>
		/// <param name="targetResource">The resource to be deleted.</param>
		public void DeleteResource(object targetResource)
		{
			_uow.AddForDelete(targetResource as IEntityCore);
		}
		

		/// <summary>
		/// Returns the instance of the resource represented by the specified resource object.
		/// </summary>
		/// <param name="resource">The object representing the resource whose instance needs to be retrieved.</param>
		/// <returns>
		/// Returns the instance of the resource represented by the specified resource object.
		/// </returns>
		public object ResolveResource(object resource)
		{
			// no need to do anything, as we don't use proxies
			return resource;
		}
		

		/// <summary>
		/// Saves all the changes that have been made by using the <see cref="T:System.Data.Services.IUpdatable"/> APIs.
		/// </summary>
		public void SaveChanges()
		{
			if(_transactionControllerCreatorFunc != null)
			{
				_uow.Commit(_transactionControllerCreatorFunc(), true);
			}
		}


		/// <summary>
		/// Cancels a change to the data.
		/// </summary>
		public void ClearChanges()
		{
			_uow.Reset();
		}


		/// <summary>
		/// Supplies the eTag value for the given entity resource.
		/// </summary>
		/// <param name="resourceCookie">Cookie that represents the resource.</param>
		/// <param name="checkForEquality">A <see cref="T:System.Boolean"/> that is true when property values must be compared for equality; false when 
		/// property values must be compared for inequality.</param>
		/// <param name="concurrencyValues">An <see cref="T:System.Collections.Generic.IEnumerable`1"/> list of the eTag property names and corresponding 
		/// values.</param>
		public void SetConcurrencyValues(object resourceCookie, bool? checkForEquality, IEnumerable<KeyValuePair<string, object>> concurrencyValues)
		{
			// not implemented, as there's no info available what on earth we're supposed to do here...
			// additionally, the ETag flag has to be set on ResourceProperty instances of fields which should be used for concurrency checking. 
			// (i.o.w.: which should get their original values set through this method)
		}
		

		/// <summary>
		/// Gets the entity collection in the entity specified where the navigator specified is mapped on (e.g. 'Orders').
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="navigatorName">Name of the navigator.</param>
		/// <returns></returns>
		/// <remarks>Lazy loading friendly method, so it won't trigger lazy loading on selfservicing and will pre-create collection on adapter.</remarks>
		private IList GetEntityCollectionForNavigator(IEntityCore entity, string navigatorName)
		{
			IList toReturn = null;
			if(entity == null)
			{
				return toReturn;
			}
			if(entity is IEntity2)
			{
				// adapter, simply read the property and return that. 
				var property = _metaDataProvider.GetCachedPropertyForEntityType(entity.GetType(), navigatorName);
				if(property != null)
				{
					toReturn = property.GetValue(entity) as IList;
				}
			}
			else
			{
				// selfservicing. 
				var relatedData = entity.GetRelatedData();
				object containedMember = null;
				if(relatedData.TryGetValue(navigatorName, out containedMember))
				{
					toReturn = containedMember as IList;
				}
			}

			return toReturn;
		}
	}
}
