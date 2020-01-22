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
using System.Collections;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System.Reflection;
using SD.LLBLGen.Pro.LinqSupportClasses;
using System.Linq.Expressions;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>
	/// Class which produces the prefetch paths for a query if the OData query contains Expand directives. 
	/// </summary>
	/// <remarks>Code based on original templates made by Brian Chance</remarks>
	[Obsolete("Starting with v4, this class is obsolete and isn't used in the service. Using it will cause v5.x WCF data services to return only the first level. ", false)]
	public class LLBLGenProODataServiceExpandProvider : IExpandProvider
	{
		/// <summary>
		/// Applies expansions to the specified <paramref name="query"/> parameter.
		/// </summary>
		/// <param name="query">The <see cref="T:System.Linq.IQueryable`1"/> object to expand.</param>
		/// <param name="expandPaths">A collection of <see cref="T:System.Data.Services.ExpandSegmentCollection"/> paths to expand.</param>
		/// <returns></returns>
		public IEnumerable ApplyExpansions(IQueryable query, ICollection<ExpandSegmentCollection> expandPaths)
		{
			if((query == null) || !typeof(IEntityCore).IsAssignableFrom(query.ElementType))
			{
				// not an entity query, expansions are not doable.
				return query;
			}

			var rootNode = new PathEdgeNode();
			foreach(ExpandSegmentCollection expandSegments in expandPaths)
			{
				DecodeSegment(rootNode, expandSegments, 0, query.ElementType);
			}
			IPathEdge[] rootPathEdges = rootNode.GetChildPathEdges();
			return typeof(LLBLGenProODataServiceExpandProvider).GetMethod("AppendWithPathCall", BindingFlags.Public | BindingFlags.Static)
								.MakeGenericMethod(query.ElementType)
								.Invoke(null, new object[] { query, rootPathEdges}) as IEnumerable;
		}


		/// <summary>
		/// Appends the WithPath call to the queryable specified
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query">The query to append WithPath to.</param>
		/// <param name="pathEdges">The path edges.</param>
		/// <returns></returns>
		/// <remarks>Declared public to avoid medium trust issues as the method is called through reflection</remarks>
		public static IEnumerable AppendWithPathCall<T>(IQueryable query, IPathEdge[] pathEdges)
			where T:class, IEntityCore
		{
			return (query as IQueryable<T>).WithPath(pathEdges);
		}


		/// <summary>
		/// Adds the segment as pathedgenode
		/// </summary>
		/// <param name="parentNode">The parent node.</param>
		/// <param name="segments">The segments.</param>
		/// <param name="currentNodeIndex">The current node index in the segments list.</param>
		/// <param name="pathElement">The path element.</param>
		/// <param name="entityType">Type of the entity.</param>
		/// <remarks>Declared public to avoid medium trust reflection issues.</remarks>
		public void AddSegment(PathEdgeNode parentNode, IList<ExpandSegment> segments, int currentNodeIndex, IPrefetchPathElementCore pathElement, Type entityType)
		{
			if(currentNodeIndex >= segments.Count)
			{
				return;
			}

			ExpandSegment segment = segments[currentNodeIndex];
			PathEdgeNode childNode = null;
			if(!parentNode.ChildPathEdgeNodes.TryGetValue(segment.Name, out childNode))
			{
				childNode = new PathEdgeNode(entityType, pathElement, segment);
				parentNode.ChildPathEdgeNodes.Add(segment.Name, childNode);
			}
			DecodeSegment(childNode, segments, ++currentNodeIndex, entityType);
		}


		/// <summary>
		/// Decodes an expansion segment.
		/// </summary>
		/// <param name="parentNode">The parent node.</param>
		/// <param name="segments">The segments.</param>
		/// <param name="currentNodeIndex">The current node index in the segments list.</param>
		/// <param name="entityType">Type of the entity.</param>
		private void DecodeSegment(PathEdgeNode parentNode, IList<ExpandSegment> segments, int currentNodeIndex, Type entityType)
		{
			if(currentNodeIndex >= segments.Count)
			{
				return;
			}
			var navigatorProperty = entityType.GetProperty(segments[currentNodeIndex].Name);
			if(navigatorProperty == null)
			{
				return;
			}
			Type navigatorEntityType = typeof(IEntityCore).IsAssignableFrom(navigatorProperty.PropertyType) 
															? navigatorProperty.PropertyType 
				                                            : navigatorProperty.PropertyType.BaseType.GetGenericArguments()[0];

			PropertyInfo prefetchPathProperty = entityType.GetProperty("PrefetchPath" + segments[currentNodeIndex].Name, BindingFlags.Static | BindingFlags.Public);
			IPrefetchPathElementCore prefetchPathElement = prefetchPathProperty.GetValue(null, null) as IPrefetchPathElementCore;
			AddSegment(parentNode, segments, currentNodeIndex, prefetchPathElement, navigatorEntityType);
		}
	}
}
