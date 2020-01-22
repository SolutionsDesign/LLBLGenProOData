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
using SD.LLBLGen.Pro.ORMSupportClasses;

using LinqExpression = System.Linq.Expressions.Expression;
using System.ComponentModel;
using SD.LLBLGen.Pro.LinqSupportClasses;
using System.Data.Services;
using System.Xml.Linq;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>
	/// Class which holds and determines the meta data of the model represented by a WCF Data Services service based on LLBLGenProODataService
	/// </summary>
	public class LLBLGenProODataServiceMetadataProvider : IDataServiceMetadataProvider
	{
		#region Class Member Declarations
		private Type _linqMetaDataType, _serviceType;
		private ILinqMetaData _linqMetaDataInstance;
		private string _containerName, _containerNamespace;
		private IElementCreatorCore _elementCreator;
		private Dictionary<string, ServiceOperation> _serviceOperations; // key is the name of the method as method overloading isn't supported.
		private Dictionary<string, ResourceSet> _resourceSets;	// key is name of the resource
		private Dictionary<string, ResourceType> _resourceTypes;	// key is name of the type.
		private Dictionary<Type, ResourceType> _resourceTypePerElementType; // key is entity / typedviewpoco type
		private Dictionary<string, ResourceType> _resourceTypePerElementName; // key is llblgen pro entity name (e.g. 'CustomerEntity') or typedview poco full type name
		private Dictionary<string, ResourceAssociationSet> _associationSetPerUniqueName; // key is created with method 'CreateUniqueRelationName'. 
		private Delegate _callGetEntityFactoryDelegate;
		private bool _allowSubTypeNavigators;
		
		/// <summary>
		/// Dictionary with type - Edm type mappings (in string).
		/// </summary>
		private static Dictionary<Type, string> _primitiveEdmMappings = new Dictionary<Type, string>();
		/// <summary>
		/// Lock object for factory delegate creator method. 
		/// </summary>
		private static object _semaphore = new object();
		#endregion

		/// <summary>
		/// Initializes the <see cref="LLBLGenProODataServiceMetadataProvider"/> class.
		/// </summary>
		static LLBLGenProODataServiceMetadataProvider()
		{
			// Copied from WCF Data Services assembly as their WebUtil class is internal.
			_primitiveEdmMappings.Add(typeof(string), "Edm.String");
			_primitiveEdmMappings.Add(typeof(bool), "Edm.Boolean"); 
			_primitiveEdmMappings.Add(typeof(bool?), "Edm.Boolean");
			_primitiveEdmMappings.Add(typeof(byte), "Edm.Byte");
			_primitiveEdmMappings.Add(typeof(byte?), "Edm.Byte");
			_primitiveEdmMappings.Add(typeof(DateTime), "Edm.DateTime");
			_primitiveEdmMappings.Add(typeof(DateTime?), "Edm.DateTime");
			_primitiveEdmMappings.Add(typeof(decimal), "Edm.Decimal");
			_primitiveEdmMappings.Add(typeof(decimal?), "Edm.Decimal");
			_primitiveEdmMappings.Add(typeof(double), "Edm.Double");
			_primitiveEdmMappings.Add(typeof(double?), "Edm.Double");
			_primitiveEdmMappings.Add(typeof(Guid), "Edm.Guid");
			_primitiveEdmMappings.Add(typeof(Guid?), "Edm.Guid");
			_primitiveEdmMappings.Add(typeof(short), "Edm.Int16");
			_primitiveEdmMappings.Add(typeof(short?), "Edm.Int16");
			_primitiveEdmMappings.Add(typeof(int), "Edm.Int32");
			_primitiveEdmMappings.Add(typeof(int?), "Edm.Int32");
			_primitiveEdmMappings.Add(typeof(long), "Edm.Int64");
			_primitiveEdmMappings.Add(typeof(long?), "Edm.Int64");
			_primitiveEdmMappings.Add(typeof(sbyte), "Edm.SByte");
			_primitiveEdmMappings.Add(typeof(sbyte?), "Edm.SByte");
			_primitiveEdmMappings.Add(typeof(float), "Edm.Single");
			_primitiveEdmMappings.Add(typeof(float?), "Edm.Single");
			_primitiveEdmMappings.Add(typeof(byte[]), "Edm.Binary");
			_primitiveEdmMappings.Add(typeof(XElement), "Edm.String");
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="LLBLGenProODataServiceMetadataProvider" /> class.
		/// </summary>
		/// <param name="serviceType">Type of the service.</param>
		/// <param name="linqMetaDataType">Type of the LinqMetaData class.</param>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="containerNamespace">The container namespace.</param>
		/// <param name="allowSubTypeNavigators">if set to <c>true</c> [allow sub type navigators].</param>
		/// <exception cref="System.ArgumentNullException">serviceType</exception>
		/// <exception cref="System.ArgumentException">containerName is empty/null;containerName</exception>
		public LLBLGenProODataServiceMetadataProvider(Type serviceType, Type linqMetaDataType, string containerName, string containerNamespace,
													  bool allowSubTypeNavigators)
		{
			if(serviceType == null)
			{
				throw new ArgumentNullException(nameof(serviceType));
			}
			if(linqMetaDataType == null)
			{
				throw new ArgumentNullException(nameof(linqMetaDataType));
			}
			if(string.IsNullOrEmpty(containerName))
			{
				throw new ArgumentException("containerName is empty/null", nameof(containerName));
			}
			_allowSubTypeNavigators = allowSubTypeNavigators;
			_serviceType = serviceType;
			_linqMetaDataType = linqMetaDataType;
			_linqMetaDataInstance = Activator.CreateInstance(_linqMetaDataType) as ILinqMetaData;
			_containerName = containerName;
			_containerNamespace = containerNamespace ?? string.Empty;
			_resourceSets = new Dictionary<string, ResourceSet>();
			_resourceTypes = new Dictionary<string, ResourceType>();
			_resourceTypePerElementType = new Dictionary<Type, ResourceType>();
			_resourceTypePerElementName = new Dictionary<string, ResourceType>();
			_associationSetPerUniqueName = new Dictionary<string, ResourceAssociationSet>();
			_serviceOperations = new Dictionary<string, ServiceOperation>();
			BuildModel();
		}
		

		/// <summary>
		/// Tries to get a resource set based on the specified name.
		/// </summary>
		/// <param name="name">Name of the <see cref="T:System.Data.Services.Providers.ResourceSet"/> to resolve.</param>
		/// <param name="resourceSet">Returns the resource set or a null value if a resource set with the given <paramref name="name"/> is not found.</param>
		/// <returns>
		/// true when resource set with the given <paramref name="name"/> is found; otherwise false.
		/// </returns>
		public bool TryResolveResourceSet(string name, out ResourceSet resourceSet)
		{
			return _resourceSets.TryGetValue(name, out resourceSet);
		}


		/// <summary>
		/// Tries to get a resource type based on the specified name.
		/// </summary>
		/// <param name="name">Name of the type to resolve.</param>
		/// <param name="resourceType">Returns the resource type or a null value if a resource type with the given <paramref name="name"/> is not found.</param>
		/// <returns>
		/// true when resource type with the given <paramref name="name"/> is found; otherwise false.
		/// </returns>
		public bool TryResolveResourceType(string name, out ResourceType resourceType)
		{
			return _resourceTypes.TryGetValue(name, out resourceType);
		}


		/// <summary>
		/// Tries to get a service operation based on the specified name.
		/// </summary>
		/// <param name="name">Name of the service operation to resolve.</param>
		/// <param name="serviceOperation">Returns the service operation or a null value if a service operation with the given <paramref name="name"/> is not found.</param>
		/// <returns>
		/// true when service operation with the given <paramref name="name"/> is found; otherwise false.
		/// </returns>
		public bool TryResolveServiceOperation(string name, out ServiceOperation serviceOperation)
		{
			return _serviceOperations.TryGetValue(name, out serviceOperation);
		}


		/// <summary>
		/// Attempts to return all types that derive from the specified resource type.
		/// </summary>
		/// <param name="resourceType">The base <see cref="T:System.Data.Services.Providers.ResourceType"/>.</param>
		/// <returns>
		/// An <see cref="T:System.Collections.Generic.IEnumerable`1"/> collection of derived <see cref="T:System.Data.Services.Providers.ResourceType"/> objects.
		/// </returns>
		public IEnumerable<ResourceType> GetDerivedTypes(ResourceType resourceType)
		{
			List<ResourceType> toReturn = new List<ResourceType>();
			var customState = resourceType.CustomState as ResourceTypeCustomState;
			if((customState != null) && (customState.InheritanceInfo!=null))
			{
				foreach(var subTypeName in customState.InheritanceInfo.EntityNamesOfPathsToLeafs)
				{
					ResourceType subtypeResourceType = null;
					if(_resourceTypePerElementName.TryGetValue(subTypeName, out subtypeResourceType))
					{
						toReturn.Add(subtypeResourceType);
					}
				}
			}
			return toReturn;
		}


		/// <summary>
		/// Gets the <see cref="T:System.Data.Services.Providers.ResourceAssociationSet"/> instance when given the source association end.
		/// </summary>
		/// <param name="resourceSet">Resource set of the source association end.</param>
		/// <param name="resourceType">Resource type of the source association end.</param>
		/// <param name="resourceProperty">Resource property of the source association end.</param>
		/// <returns>
		/// A <see cref="T:System.Data.Services.Providers.ResourceAssociationSet"/> instance.
		/// </returns>
		public ResourceAssociationSet GetResourceAssociationSet(ResourceSet resourceSet, ResourceType resourceType, ResourceProperty resourceProperty)
		{
			ResourceAssociationSet toReturn = null;
			var customState = resourceProperty.CustomState as ResourcePropertyCustomState;
			if(customState != null)
			{
				toReturn = customState.AssociationSet;
			}
			return toReturn;
		}


		/// <summary>
		/// Determines whether a resource type has derived types.
		/// </summary>
		/// <param name="resourceType">A <see cref="T:System.Data.Services.Providers.ResourceType"/> object to evaluate.</param>
		/// <returns>
		/// true when <paramref name="resourceType"/> represents an entity that has derived types; otherwise false.
		/// </returns>
		public bool HasDerivedTypes(ResourceType resourceType)
		{
			var customState = resourceType.CustomState as ResourceTypeCustomState;
			if((customState != null) && (customState.InheritanceInfo != null))
			{
				return customState.InheritanceInfo.EntityNamesOfPathsToLeafs.Any();
			}
			return false;
		}


		/// <summary>
		/// Gets the type of the factory for entity.
		/// </summary>
		/// <param name="entityType">Type of the entity.</param>
		/// <returns></returns>
		internal IEntityFactoryCore GetCachedFactoryForEntityType(Type entityType)
		{
			IEntityFactoryCore toReturn = null;
			var customState = GetResourceTypeCustomStateForEntityType(entityType);
			if(customState != null)
			{
				toReturn = customState.Factory;
			}
			return toReturn;
		}


		/// <summary>
		/// Gets the property descriptor of the name specified on the entity specified
		/// </summary>
		/// <param name="entityType">Type of the entity.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		internal PropertyDescriptor GetCachedPropertyForEntityType(Type entityType, string propertyName)
		{
			PropertyDescriptor toReturn = null;
			var customState = GetResourceTypeCustomStateForEntityType(entityType);
			if(customState != null)
			{
				toReturn = customState.GetPropertyDescriptor(propertyName);
			}
			return toReturn;
		}


		/// <summary>
		/// Gets the type of the primitive.
		/// </summary>
		/// <param name="realType">Type of the real.</param>
		/// <returns></returns>
		internal static string GetPrimitiveType(Type realType)
		{
			string toReturn = null;
			_primitiveEdmMappings.TryGetValue(realType, out toReturn);
			return toReturn;
		}


		/// <summary>
		/// Determines whether the specified type is a primitive type or not.
		/// </summary>
		/// <param name="realType">Type of the real.</param>
		/// <returns>
		/// 	<c>true</c> if [is primitive type] [the specified real type]; otherwise, <c>false</c>.
		/// </returns>
		internal static bool IsPrimitiveType(Type realType)
		{
			string primitiveType = GetPrimitiveType(realType);
			return primitiveType != null;
		}


		/// <summary>
		/// Gets the type of the entity using the typename of the entity used inside the service, which has the containername instead of the real namespace
		/// </summary>
		/// <param name="typeName">Name of the type.</param>
		/// <returns></returns>
		internal Type GetRealEntityTypeFromServiceEntityTypeName(string typeName)
		{
			Type toReturn = null;
			ResourceType resourceType = null;
			if(_resourceTypes.TryGetValue(typeName, out resourceType))
			{
				toReturn = resourceType.InstanceType;
			}
			return toReturn;
		}


		/// <summary>
		/// Gets the ResourceType associated with the entity type specified.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		internal ResourceType GetResourceTypeForEntityType(Type type)
		{
			ResourceType toReturn = null;
			_resourceTypePerElementType.TryGetValue(type, out toReturn);
			return toReturn;
		}


		/// <summary>
		/// Gets the resourcetypecustomstate object for the entity type specified
		/// </summary>
		/// <param name="entityType">Type of the entity.</param>
		/// <returns></returns>
		private ResourceTypeCustomState GetResourceTypeCustomStateForEntityType(Type entityType)
		{
			ResourceType resourceType = null;
			ResourceTypeCustomState toReturn = null;
			if(_resourceTypePerElementType.TryGetValue(entityType, out resourceType))
			{
				toReturn = resourceType.CustomState as ResourceTypeCustomState;
			}
			return toReturn;
		}


		/// <summary>
		/// Builds the model meta-data using the linq metadata type. 
		/// </summary>
		private void BuildModel()
		{
			// We're only interested in the DataSource(2)<T> returning properties, not methods.
			var queryableProperties = _linqMetaDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
																.Where(p => typeof(IQueryable).IsAssignableFrom(p.PropertyType));
			var resourceCustomStatePerElementName = new Dictionary<string, ResourceTypeCustomState>();

			// first we discover all entity / typed view types.
			foreach(var property in queryableProperties)
			{
				if((!property.PropertyType.IsGenericType) && !typeof(IQueryable).IsAssignableFrom(property.PropertyType))
				{
					continue;
				}
				var elementType = property.PropertyType.GetGenericArguments()[0];
				ProduceEntityFactoryDelegateAndElementCreator(elementType);
				var factory = GetFactory(elementType);
				IEntityCore dummyInstance = factory==null ? null : factory.Create();
				var resourceTypeCustomState = new ResourceTypeCustomState()
				{
					ElementType = elementType,
					Factory = factory,
					InheritanceInfo = dummyInstance == null ? null : dummyInstance.GetInheritanceInfo(),
					LinqMetaDataProperty = property
				};
				resourceCustomStatePerElementName[dummyInstance==null ? elementType.Name : dummyInstance.LLBLGenProEntityName] = resourceTypeCustomState;
			}

			// traverse all resource custom states we've created and recurse over supertypes to build all resourcetypes and resourcesets
			foreach(var elementName in resourceCustomStatePerElementName.Keys)
			{
				AddResourceTypeAndSet(resourceCustomStatePerElementName, elementName);
			}

			// second iteration, we discover all navigators. Because all entity types are known we can simply use the lookups
			foreach(var resourceType in _resourceTypes.Values.Distinct())
			{
				DiscoverEntityNavigators(resourceType);
			}

			// third iteration, we create association sets. We know all navigators and entity types are there, we just have to connect them together. 
			foreach(var resourceType in _resourceTypes.Values.Distinct())
			{
				CreateAssociationSets(resourceType);
			}

			DetermineServiceOperations();

			foreach(var resourceType in _resourceTypes.Values.Distinct())
			{
				resourceType.SetReadOnly();
			}
			foreach(var resourceSet in _resourceSets.Values.Distinct())
			{
				resourceSet.SetReadOnly();
			}
			foreach(var operation in _serviceOperations.Values.Distinct())
			{
				operation.SetReadOnly();
			}
		}


		/// <summary>
		/// Determines the service operation methods in the service type.
		/// </summary>
		private void DetermineServiceOperations()
		{
			if(_serviceType == null)
			{
				return;
			}

			foreach(MethodInfo info in _serviceType.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance))
			{
				var customAttributes = info.GetCustomAttributes(true);
				var webGetAttribute = customAttributes.FirstOrDefault(a => a.GetType().Name == "WebGetAttribute");
				if(webGetAttribute!=null)
				{
					AddServiceOperation(info, "GET");
				}
				else
				{
					var webInvokeAttribute = customAttributes.FirstOrDefault(a => a.GetType().Name == "WebInvokeAttribute");
					if(webInvokeAttribute!=null)
					{
						AddServiceOperation(info, "POST");
					}
				}
			}
		}


		/// <summary>
		/// Adds the service operation of the method specified with the call action specified
		/// </summary>
		/// <param name="serviceOperationMethod">The service operation method.</param>
		/// <param name="callAction">The call action.</param>
		/// <exception cref="System.InvalidOperationException">
		/// </exception>
		private void AddServiceOperation(MethodInfo serviceOperationMethod, string callAction)
		{
			ServiceOperationResultKind operationKind;
			if(_serviceOperations.ContainsKey(serviceOperationMethod.Name))
			{
				throw new InvalidOperationException(
					string.Format("WCF Data Services doesn't support method overloads for ServiceOperations. Method: '{0}.{1}'", 
								_serviceType.FullName, serviceOperationMethod.Name));
			}
			bool hasSingleResult = (serviceOperationMethod.GetCustomAttributes(typeof(SingleResultAttribute), true).Length > 0);
			
			ResourceType primitiveResourceType = null;
			if(serviceOperationMethod.ReturnType == typeof(void))
			{
				operationKind = ServiceOperationResultKind.Void;
			}
			else
			{
				Type returnType = null;
				if(LLBLGenProODataServiceMetadataProvider.IsPrimitiveType(serviceOperationMethod.ReturnType))
				{
					operationKind = ServiceOperationResultKind.DirectValue;
					primitiveResourceType = ResourceType.GetPrimitiveResourceType(serviceOperationMethod.ReturnType);
				}
				else
				{
					Type genericInterfaceElementType = GetGenericInterfaceElementType(serviceOperationMethod.ReturnType,
																					new TypeFilter(LLBLGenProODataServiceMetadataProvider.IQueryableTypeFilter));
					if(genericInterfaceElementType != null)
					{
						operationKind = hasSingleResult ? ServiceOperationResultKind.QueryWithSingleResult 
														: ServiceOperationResultKind.QueryWithMultipleResults;
						returnType = genericInterfaceElementType;
					}
					else
					{
						Type ienumerableElement = GetGenericInterfaceElementType(serviceOperationMethod.ReturnType, 
																			new TypeFilter(LLBLGenProODataServiceMetadataProvider.IEnumerableTypeFilter));
						if(ienumerableElement != null)
						{
							operationKind = ServiceOperationResultKind.Enumeration;
							returnType = ienumerableElement;
						}
						else
						{
							operationKind = ServiceOperationResultKind.DirectValue;
							returnType = serviceOperationMethod.ReturnType;
						}
					}
					primitiveResourceType = ResourceType.GetPrimitiveResourceType(returnType);
					if(primitiveResourceType == null)
					{
						_resourceTypePerElementType.TryGetValue(returnType, out primitiveResourceType);
					}
				}
				if(primitiveResourceType == null)
				{
					throw new InvalidOperationException(string.Format("Method '{0}.{1}' returns an unknown resource type '{2}'.",
																	_serviceType.FullName, serviceOperationMethod.Name, returnType.FullName));
				}
				if((operationKind == ServiceOperationResultKind.Enumeration) && hasSingleResult)
				{
					throw new InvalidOperationException(string.Format("Method '{0}.{1}' returns an enumeration but is marked as single result. Enumerations can't be single result.",
																	_serviceType.FullName, serviceOperationMethod.Name));
				}
			}
			ParameterInfo[] parameters = serviceOperationMethod.GetParameters();
			ServiceOperationParameter[] parameterArray = new ServiceOperationParameter[parameters.Length];
			for(int i = 0; i < parameterArray.Length; i++)
			{
				ParameterInfo info = parameters[i];
				if(info.IsOut || info.IsRetval)
				{
					throw new InvalidOperationException(string.Format("Method '{0}.{1}' has parameter '{2}' which isn't an [in] parameter.",
																	_serviceType.FullName, serviceOperationMethod.Name, info.Name));
				}
				ResourceType parameterType = ResourceType.GetPrimitiveResourceType(info.ParameterType);
				if(parameterType == null)
				{
					throw new InvalidOperationException(string.Format("Method '{0}.{1}', parameter '{2}' has a type '{3}' which isn't supported.",
												_serviceType.FullName, serviceOperationMethod.Name, info.Name, info.ParameterType.FullName));
				}
				string name = info.Name ?? ("p" + i);
				parameterArray[i] = new ServiceOperationParameter(name, parameterType);
			}
			ResourceSet container = null;
			if(((primitiveResourceType != null) && (primitiveResourceType.ResourceTypeKind == ResourceTypeKind.EntityType)) && 
							!this.TryFindAnyContainerForType(primitiveResourceType, out container))
			{
				throw new InvalidOperationException(string.Format("Method '{0}.{1}' returns an unknown resource type '{2}'.",
												_serviceType.FullName, serviceOperationMethod.Name, primitiveResourceType.FullName));
			}
			ServiceOperation operation = new ServiceOperation(serviceOperationMethod.Name, operationKind, primitiveResourceType, container, callAction, parameterArray);
			operation.CustomState = serviceOperationMethod;
			var mimeTypeAttribute = serviceOperationMethod.ReflectedType.GetCustomAttributes(typeof(MimeTypeAttribute), true)
												.Cast<MimeTypeAttribute>().FirstOrDefault(o =>
																{
																	return (o.MemberName == serviceOperationMethod.Name);
																});
			if(mimeTypeAttribute != null)
			{
				operation.MimeType = mimeTypeAttribute.MimeType;
			}
			_serviceOperations.Add(serviceOperationMethod.Name, operation);
		}


		/// <summary>
		/// Adds the resource type and set for the element with the name specified which can be an entity name or a typedview poco name. 
		/// If it's already present, it simply returns that resourcetype. It recurses through the names, solving supertypes first.
		/// </summary>
		/// <param name="resourceCustomStatePerElementName">Name of the resource custom state per element.</param>
		/// <param name="elementName">Name of the element.</param>
		/// <returns></returns>
		private ResourceType AddResourceTypeAndSet(Dictionary<string, ResourceTypeCustomState> resourceCustomStatePerElementName, string elementName)
		{
			ResourceTypeCustomState customState = null;
			if(!resourceCustomStatePerElementName.TryGetValue(elementName, out customState))
			{
				return null;
			}
			ResourceType resourceType = null;
			if(_resourceTypePerElementName.TryGetValue(elementName, out resourceType))
			{
				// already created
				return resourceType;
			}
			// not yet created. First check if it has a supertype. If so, create that one first. 
			ResourceType superTypeResourceType = null;
			if((customState.InheritanceInfo != null) && !string.IsNullOrEmpty(customState.InheritanceInfo.SuperTypeEntityName))
			{
				superTypeResourceType = AddResourceTypeAndSet(resourceCustomStatePerElementName, customState.InheritanceInfo.SuperTypeEntityName);
			}

			resourceType = new ResourceType(customState.ElementType, customState.IsTypedViewPoco ? ResourceTypeKind.ComplexType : ResourceTypeKind.EntityType, 
											superTypeResourceType, _containerNamespace, customState.ElementType.Name, false);
			resourceType.CanReflectOnInstanceType = true;
			_resourceTypes[resourceType.FullName] = resourceType;
			_resourceTypePerElementType[customState.ElementType] = resourceType;
			_resourceTypePerElementName.Add(customState.ElementType.Name, resourceType);
			resourceType.CustomState = customState;

			if(customState.IsTypedViewPoco)
			{
				DiscoverTypedViewFields(resourceType, customState);
			}
			else
			{
				DiscoverEntityFields(resourceType, customState);
				// subtypes don't have their own resourceset
				if((customState.InheritanceInfo != null) && !string.IsNullOrEmpty(customState.InheritanceInfo.SuperTypeEntityName))
				{
					// subtype. As it recurses over supertypes above, the hierarchy root has been handled already. So it can obtain the
					// resourceset of the hierarchy root. 
					var hierarchyRootName = customState.InheritanceInfo.EntityNamesOnHierarchyPath.FirstOrDefault() ?? string.Empty;
					ResourceType hierarchyRootResourceType = null;
					if(_resourceTypePerElementName.TryGetValue(hierarchyRootName, out hierarchyRootResourceType))
					{
						var hierarchyRootCustomState = hierarchyRootResourceType.CustomState as ResourceTypeCustomState;
						if(hierarchyRootCustomState != null)
						{
							customState.RelatedResourceSet = hierarchyRootCustomState.RelatedResourceSet;
						}
					}
				}
				else
				{
					var resourceSet = new ResourceSet(customState.LinqMetaDataProperty.Name, resourceType);
					resourceSet.CustomState = new ResourceSetCustomState() { LinqMetaDataProperty = customState.LinqMetaDataProperty };
					_resourceSets[customState.LinqMetaDataProperty.Name] = resourceSet;
					customState.RelatedResourceSet = resourceSet;
				}
			}
			return resourceType;
		}


		/// <summary>
		/// Discovers the entity fields for the entity type specified.
		/// </summary>
		/// <param name="resourceType">Type of the resource.</param>
		/// <param name="customState">State of the custom.</param>
		private void DiscoverEntityFields(ResourceType resourceType, ResourceTypeCustomState customState)
		{
			IEntityCore instance = customState.Factory.Create();
			Type entityType = customState.ElementType;
			var allProperties = TypeDescriptor.GetProperties(entityType).Cast<PropertyDescriptor>();
			var inheritedProperties = DetermineInheritedProperties(entityType, allProperties);
			var propertiesToTraverse = allProperties.Except(inheritedProperties);
			DiscoverFields(resourceType, customState, instance.Fields, propertiesToTraverse);
		}


		/// <summary>
		/// Discovers the fields of the typed view represented by the resourcetype specified.
		/// </summary>
		/// <param name="resourceType">Type of the resource.</param>
		/// <param name="customState">State of the custom.</param>
		private void DiscoverTypedViewFields(ResourceType resourceType, ResourceTypeCustomState customState)
		{
			// obtain the datasource object from the linq meta-data
			if(_linqMetaDataInstance == null)
			{
				return;
			}
			var dataSource = customState.LinqMetaDataProperty.GetValue(_linqMetaDataInstance, null) as IDataSource;
			if(dataSource == null)
			{
				return;
			}
			var typedViewType = dataSource.TypedViewEnumTypeValue;
			if(typedViewType < 0)
			{
				return;
			}
			if(_elementCreator == null)
			{
				return;
			}
			var fields = _elementCreator.GetTypedViewFields(typedViewType);
			DiscoverFields(resourceType, customState, fields, TypeDescriptor.GetProperties(customState.ElementType).Cast<PropertyDescriptor>());
		}


		/// <summary>
		/// Discovers the fields in the specified fields object and adds them as primitives to the resourcetype specified.
		/// </summary>
		/// <param name="resourceType">Type of the resource.</param>
		/// <param name="customState">State of the custom.</param>
		/// <param name="fields">The fields.</param>
		/// <param name="properties">The properties to traverse.</param>
		private void DiscoverFields(ResourceType resourceType, ResourceTypeCustomState customState, IEntityFieldsCore fields,
									IEnumerable<PropertyDescriptor> properties)
		{
			var propertiesToTraverse = properties;
			// filter out the properties which are in the IgnorePropertiesAttribute on the type, if present.
			var ignorePropertiesAttribute = customState.ElementType.GetCustomAttributes(typeof(IgnorePropertiesAttribute), false).FirstOrDefault() as IgnorePropertiesAttribute;
			if(ignorePropertiesAttribute != null)
			{
				propertiesToTraverse = propertiesToTraverse.Where(p => !ignorePropertiesAttribute.PropertyNames.Contains(p.Name)).ToList();
			}
			customState.AddProperties(propertiesToTraverse);
			foreach(PropertyDescriptor property in propertiesToTraverse)
			{
				var index = fields.GetFieldIndex(property.Name);
				if(index < 0)
				{
					// other property
					if(typeof(IEntityCore).IsAssignableFrom(property.PropertyType) || typeof(IEntityCollectionCore).IsAssignableFrom(property.PropertyType))
					{
						// entity navigator, done later
						continue;
					}
					if(!property.IsBrowsable)
					{
						continue;
					}
					AddPrimitiveProperty(resourceType, property, false);
				}
				else
				{
					var fieldInfo = fields.GetFieldInfo(index);
					AddPrimitiveProperty(resourceType, property, fieldInfo.IsPrimaryKey);
				}
			}
		}

		/// <summary>
		/// Determines the inherited properties. This method only marks a property as inherited if it belongs to another entity or to commonentitybase or
		/// classes up in the entity hierarchy.
		/// </summary>
		/// <param name="entityType">Type of the entity.</param>
		/// <param name="allProperties">All properties.</param>
		/// <returns>enumerable of property descriptors which are truly inherited from other entities.</returns>
		private IEnumerable<PropertyDescriptor> DetermineInheritedProperties(Type entityType, IEnumerable<PropertyDescriptor> allProperties)
		{
			Type baseType = entityType.BaseType;
			bool includeBaseTypeProperties = false;
			if(typeof(IEntity).IsAssignableFrom(entityType))
			{
				// selfservicing
				// check for 2-class scenario
				includeBaseTypeProperties = baseType.Name.StartsWith(entityType.Name);
			}
			else
			{
				// adapter.
				// check for 2-class scenario
				includeBaseTypeProperties = entityType.Name.Equals("My" + baseType.Name);
			}
			if(includeBaseTypeProperties)
			{
				return allProperties.Where(p => p.ComponentType != entityType && p.ComponentType != baseType);
			}
			return allProperties.Where(p => p.ComponentType != entityType);
		}


		/// <summary>
		/// Discovers the entity navigators.
		/// </summary>
		/// <param name="resourceType">Type of the resource.</param>
		private void DiscoverEntityNavigators(ResourceType resourceType)
		{
			var customState = resourceType.CustomState as ResourceTypeCustomState;
			if((customState == null) || customState.IsTypedViewPoco)
			{
				return;
			}
			var entityType = customState.ElementType;
			if(customState.Factory == null)
			{
				throw new InvalidOperationException(string.Format("Factory is null in resourceType for entity '{0}'", resourceType.Name));
			}
			var dummyInstance = customState.Factory.Create();
			var relationsWithMappedFields = dummyInstance.GetAllRelations().Where(r => !string.IsNullOrEmpty(r.MappedFieldName) && !r.IsHierarchyRelation);
			var relationPerMappedFieldName = relationsWithMappedFields.ToDictionary(r => r.MappedFieldName);

			var properties = TypeDescriptor.GetProperties(entityType).Cast<PropertyDescriptor>();
			var inheritedProperties = DetermineInheritedProperties(entityType, properties);
			foreach(PropertyDescriptor property in properties.Except(inheritedProperties))
			{
				if(!(typeof(IEntityCore).IsAssignableFrom(property.PropertyType) || typeof(IEntityCollectionCore).IsAssignableFrom(property.PropertyType)))
				{
					// not an entity navigator, 
					continue;
				}
				// relationMapped can be null, in the case of a m:n navigator.
				IEntityRelation relationMapped = null;
				relationPerMappedFieldName.TryGetValue(property.Name, out relationMapped);
				if(typeof(IEntityCore).IsAssignableFrom(property.PropertyType))
				{
					// single entity navigator
					AddNavigatorProperty(property.PropertyType, ResourcePropertyKind.ResourceReference, property.Name, resourceType, relationMapped);
					continue;
				}
				if(typeof(IEntityCollectionCore).IsAssignableFrom(property.PropertyType))
				{
					// collection navigator. We have to determine which entity type is returned from the collection. 
					// First, check if there's a TypeContainedAttribute on the property. If so, use the type in the attribute. If not,
					// try to determine the type using the linq utils method. 
					Type containedType = null;
					var typeContainedAttribute = property.Attributes[typeof(TypeContainedAttribute)] as TypeContainedAttribute;
					if((typeContainedAttribute != null) && typeContainedAttribute.TypeContainedInCollection != null)
					{
						containedType = typeContainedAttribute.TypeContainedInCollection;
					}
					else
					{
						containedType = LinqUtils.DetermineEntityTypeFromEntityCollectionType(property.PropertyType);
					}
					AddNavigatorProperty(containedType, ResourcePropertyKind.ResourceSetReference, property.Name, resourceType, relationMapped);
				}
			}
		}


		/// <summary>
		/// Creates the association sets.
		/// </summary>
		/// <param name="resourceType">Type of the resource.</param>
		/// <remarks>M:N relations have no associations, as they're read only in llblgen pro.</remarks>
		private void CreateAssociationSets(ResourceType resourceType)
		{
			var navigatorProperties = GetNavigatorProperties(resourceType);
			var resourceSet = ((ResourceTypeCustomState)resourceType.CustomState).RelatedResourceSet;
			foreach(var navigatorProperty in navigatorProperties)
			{
				var customState = navigatorProperty.CustomState as ResourcePropertyCustomState;
				if(customState == null)
				{
					continue;
				}
				string uniqueName = string.Empty;
				var mappedRelation = customState.MappedRelation;
				if(mappedRelation == null)
				{
					// m:n, create other unique name.
					uniqueName = resourceType.Name + "_" + navigatorProperty.Name;
				}
				else
				{
					uniqueName = CreateUniqueRelationName(mappedRelation);
				}
				if(_associationSetPerUniqueName.ContainsKey(uniqueName))
				{
					// already processed when processing the opposite side. 
					continue;
				}
				ResourceType oppositeResourceType = null;
				ResourceProperty oppositeNavigator = null;
				if(mappedRelation == null)
				{
					_resourceTypePerElementType.TryGetValue(customState.RelatedEntityType, out oppositeResourceType);
				}
				else
				{
					FindOppositeNavigator(mappedRelation, out oppositeResourceType, out oppositeNavigator);
					if(oppositeResourceType == null)
					{
						continue;
					}
				}
				ResourceSet oppositeResourceSet = ((ResourceTypeCustomState)oppositeResourceType.CustomState).RelatedResourceSet;
				var association = new ResourceAssociationSet(uniqueName,
						new ResourceAssociationSetEnd(resourceSet, resourceType, navigatorProperty),
						new ResourceAssociationSetEnd(oppositeResourceSet, oppositeResourceType, oppositeNavigator));
				_associationSetPerUniqueName[uniqueName] = association;
				((ResourcePropertyCustomState)navigatorProperty.CustomState).AssociationSet = association;
				if(oppositeNavigator != null)
				{
					((ResourcePropertyCustomState)oppositeNavigator.CustomState).AssociationSet = association;
				}
			}
		}
		

		/// <summary>
		/// Finds the opposite navigator of the relation specified. If not found, the opposite navigator is null.
		/// </summary>
		/// <param name="relation">The relation.</param>
		/// <param name="oppositeResourceType">Type of the opposite resource.</param>
		/// <param name="oppositeNavigator">The opposite navigator.</param>
		private void FindOppositeNavigator(IEntityRelation relation, out ResourceType oppositeResourceType, 
										  out ResourceProperty oppositeNavigator)
		{
			oppositeNavigator = null;
			var oppositeEntityName = relation.StartEntityIsPkSide ? relation.GetFKEntityFieldCore(0).ActualContainingObjectName : relation.GetPKEntityFieldCore(0).ActualContainingObjectName;

			if(!_resourceTypePerElementName.TryGetValue(oppositeEntityName, out oppositeResourceType))
			{
				// not found
				return;
			}

			// traverse fields. They should be equal
			List<IEntityFieldCore> fkFieldsRelation = relation.GetAllFKEntityFieldCoreObjects();
			List<IEntityFieldCore> pkFieldsRelation = relation.GetAllPKEntityFieldCoreObjects();
			foreach(var oppositeNavigatorCandidate in GetNavigatorProperties(oppositeResourceType))
			{
				var oppositeCustomState = oppositeNavigatorCandidate.CustomState as ResourcePropertyCustomState;
				IEntityRelation oppositeRelation = oppositeCustomState.MappedRelation;
				if(oppositeRelation == null)
				{
					continue;
				}
				// compare type of relation. if types aren't opposites, candidate isn't the one we're looking for. 
				switch(relation.TypeOfRelation)
				{
					case RelationType.ManyToOne:
						if(oppositeRelation.TypeOfRelation != RelationType.OneToMany)
						{
							continue;
						}
						break;
					case RelationType.OneToMany:
						if(oppositeRelation.TypeOfRelation != RelationType.ManyToOne)
						{
							continue;
						}
						break;
					case RelationType.OneToOne:
						if(oppositeRelation.TypeOfRelation != RelationType.OneToOne)
						{
							continue;
						}
						break;
					default:
						continue;
				}

				List<IEntityFieldCore> fkFieldsCandidate = oppositeRelation.GetAllFKEntityFieldCoreObjects();
				List<IEntityFieldCore> pkFieldsCandidate = oppositeRelation.GetAllPKEntityFieldCoreObjects();
				// compare pk fields of candidate with pk fields of relation and vice versa. 
				if((fkFieldsCandidate.Count != fkFieldsRelation.Count) || (pkFieldsCandidate.Count != pkFieldsRelation.Count))
				{
					// # of field don't match
					continue;
				}
				bool areEqual = true;
				for(int i = 0; i < fkFieldsRelation.Count; i++)
				{
					if(((IComparable)fkFieldsRelation[i]).CompareTo(fkFieldsCandidate[i]) != 0)
					{
						// already not equal.
						areEqual = false;
						break;
					}
				}
				if(!areEqual)
				{
					continue;
				}
				for(int i = 0; i < pkFieldsRelation.Count; i++)
				{
					if(((IComparable)pkFieldsRelation[i]).CompareTo(pkFieldsCandidate[i]) != 0)
					{
						// already not equal.
						areEqual = false;
						break;
					}
				}
				if(areEqual)
				{
					// found opposite
					oppositeNavigator = oppositeNavigatorCandidate;
					break;
				}
			}
		}


		/// <summary>
		/// Creates a unique name for the relation specified. The name is: PkEntityNameFkEntityName.FkFields. The FkFields are joined with a |
		/// </summary>
		/// <param name="relation">The relation.</param>
		/// <returns></returns>
		private string CreateUniqueRelationName(IEntityRelation relation)
		{
			return relation.GetPKEntityFieldCore(0).ActualContainingObjectName + relation.GetFKEntityFieldCore(0).ActualContainingObjectName +
						string.Join("", relation.GetAllFKEntityFieldCoreObjects().Select(f => f.Name));
		}
		

		/// <summary>
		/// Adds the navigator property.
		/// </summary>
		/// <param name="relatedEntityType">Type of the related entity.</param>
		/// <param name="propertyKind">Kind of the property.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="containerType">Type of the container.</param>
		/// <param name="mappedEntityRelation">The mapped entity relation.</param>
		/// <remarks>If _allowSubTypeNavigators is set to false, the provider switches to ODatav2: 
		/// Due to a limitation in WCF Data Services, it will only add the navigator if the containerType isn't a subtype. 
		/// If it's a subtype, it will ignore the navigator. Also, it will only add the navigator when the containerType's resourceSet and the 
		/// relatedEntityType's resourceset are not equal in the situation if one or both are subtypes. WCF Data Services can't deal with navigators 
		/// which point to a resourcetype which is in the same resourceset and which is a subtype. 
		/// If one side is a subtype and both sides are in the same resourceset this limitation is hit and the navigator isn't added.</remarks>
		private void AddNavigatorProperty(Type relatedEntityType, ResourcePropertyKind propertyKind, string propertyName, ResourceType containerType, 
											IEntityRelation mappedEntityRelation)
		{
			ResourceType navigatorType = null;
			if(!_resourceTypePerElementType.TryGetValue(relatedEntityType, out navigatorType))
			{
				return;
			}
			var customStateContainerType = containerType.CustomState as ResourceTypeCustomState;
			var customStateNavigatorType = navigatorType.CustomState as ResourceTypeCustomState;
			if((customStateContainerType == null) || (customStateNavigatorType == null))
			{
				return;
			}
			// check for limitation in WCF Data Services (see remarks above)
			bool containerTypeIsSubtype = false;
			if(customStateContainerType.InheritanceInfo != null)
			{
				containerTypeIsSubtype = !string.IsNullOrEmpty(customStateContainerType.InheritanceInfo.SuperTypeEntityName);
			}
			if(containerTypeIsSubtype && !_allowSubTypeNavigators)
			{
				// container is subtype-> ignore, as WCF Data Services can't deal with navigators on subtypes.
				return;
			}
			bool navigatorTypeIsSubtype = false;
			if(customStateNavigatorType.InheritanceInfo != null)
			{
				navigatorTypeIsSubtype = !string.IsNullOrEmpty(customStateNavigatorType.InheritanceInfo.SuperTypeEntityName);
			}
			if(!_allowSubTypeNavigators && navigatorTypeIsSubtype && 
					(customStateNavigatorType.RelatedResourceSet == customStateContainerType.RelatedResourceSet))
			{
				// one or both sides are subtypes and the related resourceset is equal -> limitation hit, ignore navigator
				return;
			}
			// add navigator 
			containerType.AddProperty(new ResourceProperty(propertyName, propertyKind, navigatorType)
			{
				CustomState = new ResourcePropertyCustomState() { MappedRelation = mappedEntityRelation, RelatedEntityType = relatedEntityType }
			});
		}


		/// <summary>
		/// Adds the primitive property to the resourcetype specified.
		/// </summary>
		/// <param name="resourceType">Type of the resource.</param>
		/// <param name="property">The property.</param>
		/// <param name="isPrimaryKey">if set to <c>true</c> [is primary key].</param>
		private void AddPrimitiveProperty(ResourceType resourceType, PropertyDescriptor property, bool isPrimaryKey)
		{
			var type = ResourceType.GetPrimitiveResourceType(property.PropertyType);
			if(type == null)
			{
				// apparently a type which can't be handled by WCF Data Services. Enums and other types not supported by EDMX/Edm are candidates for this. 
				// Ignore property
				return;
			}
			var kind = ResourcePropertyKind.Primitive;
			// Keys of type binary are not supported by WCF Data Services. 
			bool isBinaryField = typeof(byte[]).IsAssignableFrom(property.PropertyType);
			if(isPrimaryKey && !isBinaryField)
			{
				kind |= ResourcePropertyKind.Key;
			}
			var resourceProperty = new ResourceProperty(property.Name, kind, type);
			resourceProperty.CanReflectOnInstanceTypeProperty = true;
			resourceType.AddProperty(resourceProperty);
		}

		
		/// <summary>
		/// Gets the factory for the entity with the type specified.
		/// </summary>
		/// <param name="elementType">Type of the element.</param>
		/// <returns>the factory for the element specified, or null if not found</returns>
		private IEntityFactoryCore GetFactory(Type elementType)
		{
			if(_callGetEntityFactoryDelegate == null)
			{
				return null;
			}
			return (IEntityFactoryCore)_callGetEntityFactoryDelegate.DynamicInvoke(elementType);
		}


		/// <summary>
		/// Produces the entity factory delegate and obtains the element creator instance, through reflection.
		/// </summary>
		/// <param name="elementType">Type of the element.</param>
		private void ProduceEntityFactoryDelegateAndElementCreator(Type elementType)
		{
			if(typeof(IEntityCore).IsAssignableFrom(elementType) && (_callGetEntityFactoryDelegate == null))
			{
				lock(_semaphore)
				{
					if(_callGetEntityFactoryDelegate == null)
					{
						// create the delegate for invoking the entity factory factory.
						string rootNamespace = elementType.FullName.Substring(0, elementType.FullName.Length - (elementType.Name.Length + ".EntityClasses".Length) - 1);
						string generalEntityFactoryFactoryTypeName = rootNamespace + ".FactoryClasses.EntityFactoryFactory";
						Type generalEntityFactoryFactoryType = elementType.Assembly.GetType(generalEntityFactoryFactoryTypeName);
						if(generalEntityFactoryFactoryType == null)
						{
							return;
						}
						var methodInfo = generalEntityFactoryFactoryType.GetMethod("GetFactory", BindingFlags.Static | BindingFlags.Public, null,
																					new Type[] { typeof(Type) }, null);

						if(methodInfo == null)
						{
							return;
						}
						var parameter = LinqExpression.Parameter(typeof(Type), "p");
						var methodCallExpression = LinqExpression.Call(methodInfo, parameter);
						var lambda = LinqExpression.Lambda(methodCallExpression, parameter);
						_callGetEntityFactoryDelegate = lambda.Compile();

						// obtain elementcreator class
						string elementCreatorTypeName = rootNamespace + ".FactoryClasses.ElementCreator";
						Type elementCreatorType = elementType.Assembly.GetType(elementCreatorTypeName);
						if(elementCreatorType != null)
						{
							_elementCreator = Activator.CreateInstance(elementCreatorType) as IElementCreatorCore;
						}
					}
				}
			}
		}


		/// <summary>
		/// Gets the navigator properties.
		/// </summary>
		/// <param name="resourceType">Type of the resource.</param>
		/// <returns></returns>
		private static IEnumerable<ResourceProperty> GetNavigatorProperties(ResourceType resourceType)
		{
			return resourceType.PropertiesDeclaredOnThisType
									.Where(p => p.Kind == ResourcePropertyKind.ResourceReference || p.Kind == ResourcePropertyKind.ResourceSetReference);
		}
		

		/// <summary>
		/// Gets the type of the generic interface element.
		/// </summary>
		/// <param name="interfaceType">The type.</param>
		/// <param name="typeFilter">The type filter.</param>
		/// <returns></returns>
		private static Type GetGenericInterfaceElementType(Type interfaceType, TypeFilter typeFilter)
		{
			if(typeFilter(interfaceType, null))
			{
				return interfaceType.GetGenericArguments()[0];
			}
			Type[] typeArray = interfaceType.FindInterfaces(typeFilter, null);
			if(typeArray.Length == 1)
			{
				return typeArray[0].GetGenericArguments()[0];
			}
			return null;
		}


		/// <summary>
		/// IQueryable type filter method, used in service operation retrieval
		/// </summary>
		/// <param name="type">The m.</param>
		/// <param name="filterCriteria">The filter criteria.</param>
		/// <returns></returns>
		private static bool IQueryableTypeFilter(Type type, object filterCriteria)
		{
			return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IQueryable<>)));
		}


		/// <summary>
		/// IEnumerable type filter method, used in service operation retrieval
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="filterCriteria">The filter criteria.</param>
		/// <returns></returns>
		private static bool IEnumerableTypeFilter(Type type, object filterCriteria)
		{
			return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
		}


		/// <summary>
		/// Tries the type of the find any container for.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="container">The container.</param>
		/// <returns></returns>
		private bool TryFindAnyContainerForType(ResourceType type, out ResourceSet container)
		{
			foreach(ResourceSet set in _resourceSets.Values)
			{
				if(ResourceTypeIsAssignableFrom(set.ResourceType, type))
				{
					container = set;
					return true;
				}
			}
			container = null;
			return false;
		}


		/// <summary>
		/// Determines whether the resource type specified is a supertype of the base type specified.
		/// </summary>
		/// <param name="baseType">Type of the base.</param>
		/// <param name="superType">Type of the super.</param>
		/// <returns></returns>
		private static bool ResourceTypeIsAssignableFrom(ResourceType baseType, ResourceType superType)
		{
			while(superType != null)
			{
				if (superType == baseType)
				{
					return true;
				}
				superType = superType.BaseType;
			}
			return false;
		}

		
		#region Class Property Declarations
		/// <summary>
		/// Container name for the data source.
		/// </summary>
		public string ContainerName
		{
			get { return _containerName; }
		}

		/// <summary>
		/// Namespace name for the data source.  This is used in the $metadata response. 
		/// </summary>
		public string ContainerNamespace
		{
			get { return _containerNamespace; }
		}

		/// <summary>
		/// Gets all available containers.
		/// </summary>
		/// <value></value>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1"/> collection of <see cref="T:System.Data.Services.Providers.ResourceSet"/> objects.</returns>
		public IEnumerable<ResourceSet> ResourceSets
		{
			get { return this._resourceSets.Values; }
		}

		/// <summary>
		/// Returns all the service operations in this data source.
		/// </summary>
		/// <value></value>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1"/> collection of <see cref="T:System.Data.Services.Providers.ServiceOperation"/> objects.</returns>
		public IEnumerable<ServiceOperation> ServiceOperations
		{
			get { return this._serviceOperations.Values; }
		}

		/// <summary>
		/// Returns all the types in this data source.
		/// </summary>
		/// <value></value>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1"/> collection of <see cref="T:System.Data.Services.Providers.ResourceType"/> objects.</returns>
		public IEnumerable<ResourceType> Types
		{
			get { return _resourceTypes.Values; }
		}
		#endregion
	}
}
