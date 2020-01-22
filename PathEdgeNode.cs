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
using System.Data.Services;
using System.Reflection;
using SD.LLBLGen.Pro.LinqSupportClasses;
using System.Linq.Expressions;

namespace SD.LLBLGen.Pro.ODataSupportClasses
{
	/// <summary>Container class for pathedge information</summary>
	/// <remarks>Declared public to avoid medium trust reflection issues as reflection is used to call generic method variants.</remarks>
	public class PathEdgeNode
	{
		#region Class Member declarations
		private IPrefetchPathElementCore _pathElement;
		private ExpandSegment _segment;
		private Dictionary<string, PathEdgeNode> _childPathEdgeNodes = new Dictionary<string, PathEdgeNode>();
		private Type _entityType;
		private MethodInfo _pathEdgeCreator;
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="PathEdgeNode"/> class.
		/// </summary>
		internal PathEdgeNode()
			: this(null, null, null)
		{
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="PathEdgeNode"/> class.
		/// </summary>
		/// <param name="entityType">Type of the entity.</param>
		/// <param name="pathElement">The path element.</param>
		/// <param name="segment">The segment.</param>
		internal PathEdgeNode(Type entityType, IPrefetchPathElementCore pathElement, ExpandSegment segment)
		{
			_pathElement = pathElement;
			_segment = segment;
			_entityType = entityType;
		}


		/// <summary>Gets the path edge.</summary>
		/// <returns>the path edge instance represented by this node</returns>
		public IPathEdge GetPathEdge()
		{
			if(_pathEdgeCreator == null)
			{
				var getPathEdgeMethod = typeof(PathEdgeNode).GetMethod("GetPathEdgeGeneric", BindingFlags.Public | BindingFlags.Instance);
				_pathEdgeCreator = getPathEdgeMethod.MakeGenericMethod(_entityType);
			}
			return _pathEdgeCreator.Invoke(this, null) as IPathEdge;
		}


		/// <summary>
		/// Gets the path edge.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IPathEdge GetPathEdgeGeneric<T>()
			where T : class, IEntityCore
		{
			return new PathEdge<T>(_pathElement,
									((_segment != null) && _segment.HasFilter) ? _segment.Filter as Expression<Func<T, bool>> : null,
									GetChildPathEdges());
		}


		/// <summary>Gets the child path edges.</summary>
		/// <returns>array of the patch edges requested</returns>
		public IPathEdge[] GetChildPathEdges()
		{
			List<IPathEdge> edges = new List<IPathEdge>();
			foreach(PathEdgeNode childnode in _childPathEdgeNodes.Values)
			{
				edges.Add(childnode.GetPathEdge());
			}
			return edges.ToArray();
		}


		#region Class Property Declarations
		/// <summary>Gets the child path edge nodes.</summary>
		public Dictionary<string, PathEdgeNode> ChildPathEdgeNodes
		{
			get { return _childPathEdgeNodes; }
		}
		#endregion
	}
}
