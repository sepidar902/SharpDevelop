// 
// CreateEventInvocator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class CreateEventInvocator : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			VariableInitializer initializer;
			var eventDeclaration = GetEventDeclaration (context, out initializer);
			if (eventDeclaration == null)
				return false;
			var type = (TypeDeclaration)eventDeclaration.Parent;
			return !type.Members.Any (m => m is MethodDeclaration && ((MethodDeclaration)m).Name == "On" + initializer.Name);
		}
		
		public void Run (RefactoringContext context)
		{
			VariableInitializer initializer;
			var eventDeclaration = GetEventDeclaration (context, out initializer);
			var type = context.Resolve (eventDeclaration.ReturnType).Type;
			if (type == null)
				return;
			var invokeMethod = type.GetDelegateInvokeMethod ();
			if (invokeMethod == null)
				return;
			
			bool hasSenderParam = false;
			IEnumerable<IParameter> pars = invokeMethod.Parameters;
			if (invokeMethod.Parameters.Any ()) {
				var first = invokeMethod.Parameters [0];
				if (first.Name == "sender" /*&& first.Type == "System.Object"*/) {
					hasSenderParam = true;
					pars = invokeMethod.Parameters.Skip (1);
				}
			}
			const string handlerName = "handler";
					
			var arguments = new List<Expression> ();
			if (hasSenderParam)
				arguments.Add (new ThisReferenceExpression ());
			foreach (var par in pars)
				arguments.Add (new IdentifierExpression (par.Name));
			
			var methodDeclaration = new MethodDeclaration () {
				Name = "On" + initializer.Name,
				ReturnType = context.CreateShortType (eventDeclaration.ReturnType),
				Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Protected | ICSharpCode.NRefactory.CSharp.Modifiers.Virtual,
				Body = new BlockStatement () {
					new VariableDeclarationStatement (context.CreateShortType (eventDeclaration.ReturnType), handlerName, new MemberReferenceExpression (new ThisReferenceExpression (), initializer.Name)),
					new IfElseStatement () {
						Condition = new BinaryOperatorExpression (new IdentifierExpression (handlerName), BinaryOperatorType.InEquality, new PrimitiveExpression (null)),
						TrueStatement = new ExpressionStatement (new InvocationExpression (new IdentifierExpression (handlerName), arguments))
					}
				}
			};
			
			foreach (var par in pars) {
				var typeName = context.CreateShortType (par.Type.Resolve (context.TypeResolveContext));
				var decl = new ParameterDeclaration (typeName, par.Name);
				methodDeclaration.Parameters.Add (decl);
			}
			
			using (var script = context.StartScript ()) {
				script.InsertWithCursor ("Create event invocator", methodDeclaration, Script.InsertPosition.After);
			}
		}
		
		static EventDeclaration GetEventDeclaration (RefactoringContext context, out VariableInitializer initializer)
		{
			var result = context.GetNode<EventDeclaration> ();
			if (result == null) {
				initializer = null;
				return null;
			}
			initializer = result.Variables.First (v => v.NameToken.Contains (context.Location));
			return initializer != null ? result : null;
		}
	}
}

