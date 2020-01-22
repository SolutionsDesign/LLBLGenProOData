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
using SD.LLBLGen.Pro.ORMSupportClasses;
using System.Data.Services.Providers;
using System.ComponentModel;
using System.Reflection;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>
	/// Simple bucket class which holds the custom state of a ResourceType
	/// </summary>
	internal class ResourceTypeCustomState
	{
		#region Class Member Declarations
		private Dictionary<string, PropertyDescriptor> _elementProperties;
		#endregion

		/// <summary>
		/// Adds all the properties specified which are browsable to this custom state.
		/// </summary>
		/// <param name="discoveredProperties">The discovered properties.</param>
		internal void AddProperties(IEnumerable<PropertyDescriptor> discoveredProperties)
		{
			_elementProperties = new Dictionary<string, PropertyDescriptor>();
			foreach(PropertyDescriptor descriptor in discoveredProperties)
			{
				if(!descriptor.IsBrowsable)
				{
					continue;
				}
				_elementProperties[descriptor.Name] = descriptor;
			}
		}

		/// <summary>
		/// Gets the property descriptor.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		internal PropertyDescriptor GetPropertyDescriptor(string name)
		{
			PropertyDescriptor toReturn = null;
			_elementProperties.TryGetValue(name, out toReturn);
			return toReturn;
		}


		#region Class Property Declarations
		/// <summary>
		/// Gets or sets the related linq meta data property.
		/// </summary>
		internal PropertyInfo LinqMetaDataProperty { get; set; }
		/// <summary>
		/// Gets or sets the type of the element, be it the entity type or the typedview poco type.
		/// </summary>
		internal Type ElementType { get; set; }
		/// <summary>
		/// Gets or sets the factory. Only set if the element type is an entity.
		/// </summary>
		internal IEntityFactoryCore Factory { get; set; }
		internal bool IsTypedViewPoco
		{
			get { return this.Factory == null; }
		}
		/// <summary>
		/// Gets or sets the related resource set. Only set when ElementType is an entity.
		/// </summary>
		internal ResourceSet RelatedResourceSet { get; set; }
		/// <summary>
		/// Gets or sets the inheritance info. Only set when elementtype is an entity.
		/// </summary>
		internal IInheritanceInfo InheritanceInfo { get; set; }
		#endregion
	}
}
