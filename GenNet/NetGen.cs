using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Validation;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace gen
{




	public class NetViewGen : ICodeGenerator
	{
		private readonly AttributeData m_attributeData;
		private readonly ImmutableDictionary<string, TypedConstant> m_data;

		private TransformationContext m_context;

		private Dictionary<VariableDeclaratorSyntax, TypeSyntax> m_fields = new Dictionary<VariableDeclaratorSyntax, TypeSyntax>();

		private ClassDeclarationSyntax m_class;

		private INamedTypeSymbol m_sym;

		private INamedTypeSymbol m_baseSym;

		private Dictionary<string, INamedTypeSymbol> m_dictClasses = new Dictionary<string, INamedTypeSymbol>();




		public NetViewGen( AttributeData attributeData )
		{
			Requires.NotNull( attributeData, nameof( attributeData ) );

			m_attributeData = attributeData;
			m_data = attributeData.NamedArguments.ToImmutableDictionary( kv => kv.Key, kv => kv.Value );
		}


		public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync( TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken )
		{
			m_context = context;

			m_class = m_context.ProcessingNode as ClassDeclarationSyntax;

			m_sym = m_context.SemanticModel.GetDeclaredSymbol( m_class );

			m_baseSym = m_sym.BaseType;

			var baseIsObject = m_baseSym.SpecialType == SpecialType.System_Object;


			var results = SF.List<MemberDeclarationSyntax>();

			ClassDeclarationSyntax copy = null;

			var applyToClassIdentifier = m_class.Identifier;

			if( m_class != null )
			{
				var fieldsComment = "";

				foreach( var member in m_class.Members )
				{
					var field = member as FieldDeclarationSyntax;
					if( field != null )
					{
						var decl = field.Declaration;

						var type = decl.Type;

						foreach( var v in decl.Variables )
						{
							m_fields.Add( v, type );
							fieldsComment += $"({type.GetType().Name}){type} ({v.GetType().Name}){v}\n\r";
						}
					}
				}

				var leadingTrivia = $"{fieldsComment}\r\n";
				var trailingTrivia = $"";

				leadingTrivia += $"\r\nViewers\r\n";

				foreach( var mem in m_context.Compilation.GlobalNamespace.GetMembers() )
				{
					GetClasses( ref m_dictClasses, mem );
				}

				leadingTrivia += $"Total Classes {m_dictClasses.Count}\r\n";


				foreach( var kvp in m_dictClasses )
				{
					if( kvp.Key.Contains( "View" ) )
					{
						var type = kvp.Value;

						if( type.ContainingNamespace.Name == "net" )
						{
							if( type.BaseType.Name == "View" )
							{
								leadingTrivia += $"View class found {kvp.Value.ToDisplayString()}\r\n";
							}

						}
					}
				}



				//var newUsing = SF.UsingDirective( SF.IdentifierName( "" ) );
				SyntaxList<MemberDeclarationSyntax> withMembers = new SyntaxList<MemberDeclarationSyntax>();

				try
				{
					withMembers = CreateDefault();

				}
				catch(Exception ex)
				{
					trailingTrivia += $"Exception running CreateDefault [{ex.Message}]";
				}


				if( baseIsObject )
				{
					withMembers = withMembers.AddRange( CreateVersion() );
				}

				withMembers = withMembers.AddRange( CreateProtectedConstructors() );


				try
				{
					withMembers = withMembers.AddRange( CreateWithFunctions() );

				}
				catch( Exception ex )
				{
					trailingTrivia += $"Exception running CreateWithFunctions [{ex.Message}]";
				}



				withMembers = withMembers.AddRange( CreateCreateFunctions() );

				var leadingTriviaComment = SF.Comment( $"/*\r\n{leadingTrivia}\r\n*/" );
				var trailingTriviaComment = SF.Comment( $"/*\r\n{trailingTrivia}\r\n*/" );

				copy = SF.ClassDeclaration( applyToClassIdentifier )
					.WithTypeParameterList( m_class.TypeParameterList )
					.WithModifiers( SyntaxTokenList.Create( SF.Token( SyntaxKind.PartialKeyword ) ) )
					.WithLeadingTrivia( leadingTriviaComment )
					.WithTrailingTrivia( trailingTriviaComment )
					.WithMembers( withMembers );

			}
			else
			{
				// TODO ERROR 
			}


			if( copy != null )
			{
				results = results.Add( copy );
			}

			return Task.FromResult( results );
		}

		void GetClasses( ref Dictionary<string, INamedTypeSymbol> dictClasses, INamespaceOrTypeSymbol sym )
		{
			switch( sym.Kind )
			{
				//case SymbolKind.Alias:	return;
				case SymbolKind.ArrayType:
				return;
				//case SymbolKind.Assembly:	return;
				case SymbolKind.DynamicType:
				return;
				case SymbolKind.ErrorType:
				return;
				case SymbolKind.Event:
				return;
				case SymbolKind.Field:
				return;
				case SymbolKind.Label:
				return;
				//case SymbolKind.Local:	return;
				case SymbolKind.Method:
				return;
				//case SymbolKind.NetModule:	return;
				case SymbolKind.NamedType:
				{
					var namedType = sym as INamedTypeSymbol;

					var namespaceName = namedType.ContainingNamespace?.Name ?? "";

					var name = $"{namespaceName}.{namedType.Name}";

					if( namedType.IsType && !dictClasses.ContainsKey( name ) )
					{
						dictClasses.Add( name, namedType );
					}

					foreach( var subsym in namedType.GetTypeMembers() )
					{
						GetClasses( ref dictClasses, subsym );
					}

				}
				return;
				//case SymbolKind.Namespace:	return;
				case SymbolKind.Parameter:
				return;
				case SymbolKind.PointerType:
				return;
				case SymbolKind.Property:
				return;
				case SymbolKind.RangeVariable:
				return;
				//case SymbolKind.TypeParameter:	return;
				case SymbolKind.Preprocessing:
				return;
				case SymbolKind.Discard:
				return;
			}


			if( sym is IAssemblySymbol assem )
			{

			}

			if( sym is INamespaceSymbol nspace )
			{
				foreach( var subsym in nspace.GetMembers() )
				{
					GetClasses( ref dictClasses, subsym );
				}

			}

		}


		void AddMembers( ref string diag, INamespaceOrTypeSymbol thingy, int depth )
		{
			switch( thingy.Kind )
			{
				case SymbolKind.Alias:
				return;
				case SymbolKind.ArrayType:
				return;
				//case SymbolKind.Assembly:	return;
				case SymbolKind.DynamicType:
				return;
				case SymbolKind.ErrorType:
				return;
				case SymbolKind.Event:
				return;
				case SymbolKind.Field:
				return;
				case SymbolKind.Label:
				return;
				case SymbolKind.Local:
				return;
				case SymbolKind.Method:
				return;
				//case SymbolKind.NetModule:	return;
				//case SymbolKind.NamedType:	return;
				//case SymbolKind.Namespace:	return;
				case SymbolKind.Parameter:
				return;
				case SymbolKind.PointerType:
				return;
				case SymbolKind.Property:
				return;
				case SymbolKind.RangeVariable:
				return;
				case SymbolKind.TypeParameter:
				return;
				case SymbolKind.Preprocessing:
				return;
				case SymbolKind.Discard:
				return;
			}


			for( var i = 0; i < depth; ++i )
			{
				diag += "| ";
			}

			foreach( var sym in thingy.GetMembers() )
			{
				switch( sym.Kind )
				{
					case SymbolKind.Alias:
					continue;
					case SymbolKind.ArrayType:
					continue;
					//case SymbolKind.Assembly:	continue;
					case SymbolKind.DynamicType:
					continue;
					case SymbolKind.ErrorType:
					continue;
					case SymbolKind.Event:
					continue;
					case SymbolKind.Field:
					continue;
					case SymbolKind.Label:
					continue;
					case SymbolKind.Local:
					continue;
					case SymbolKind.Method:
					continue;
					//case SymbolKind.NetModule:	continue;
					//case SymbolKind.NamedType:	continue;
					//case SymbolKind.Namespace:	continue;
					case SymbolKind.Parameter:
					continue;
					case SymbolKind.PointerType:
					continue;
					case SymbolKind.Property:
					continue;
					case SymbolKind.RangeVariable:
					continue;
					case SymbolKind.TypeParameter:
					continue;
					case SymbolKind.Preprocessing:
					continue;
					case SymbolKind.Discard:
					continue;
				}

				diag += $"{sym.Kind}/{sym.GetType().Name} | {sym.Name} | {sym.ToDisplayString()} | ";

				if( sym is IAssemblySymbol assem )
				{
					diag += $"{assem.Name} | {assem.Identity.Name} | {String.Join( ", ", assem.Locations )}\r\n";
				}

				var ranChildren = false;

				if( sym is ITypeSymbol type )
				{
					diag += $"{type.TypeKind}\r\n";

					ranChildren = true;

					AddMembers( ref diag, type, depth + 1 );
				}

				if( sym is INamespaceOrTypeSymbol namespaceOrType )
				{
					diag += $"{namespaceOrType.Name}";

					if( !ranChildren )
					{
						AddMembers( ref diag, namespaceOrType, depth + 1 );
					}

					ranChildren = true;
				}

				//| Types: {sym.GetTypeMembers().Length} | Members: {sym.GetMembers().Length}\r\n

			}
		}

		SyntaxToken ModifyToken( string mod, SyntaxToken token )
		{
			return SF.IdentifierName( String.Format( mod, token.Text ) ).Identifier;
		}

		IdentifierNameSyntax ModifyIdentifier( string mod, SyntaxToken token )
		{
			return SF.IdentifierName( String.Format( mod, token ) );
		}





		SyntaxList<StatementSyntax> CreateAssignments( string varPrefix, string version )
		{
			var assignments = new SyntaxList<StatementSyntax>();

			var constructorParams = $"{version}";

			var first = true;
			foreach( var f in m_fields )
			{
				//if( !first )
				{
					constructorParams += ", ";
				}
				/*
				else
				{
					first = false;
				}
				*/

				var statement = SF.ParseStatement( $"var {f.Key.Identifier}New = {f.Key.Identifier}Opt.ValueOr({varPrefix}{f.Key.Identifier});" );

				assignments = assignments.Add( statement );

				constructorParams += ModifyToken( "{0}New", f.Key.Identifier );

			}

			var retExp = SF.ParseExpression( $"new {SU.ClassNameWithGenerics(m_class)}( {constructorParams} )" );

			var ret = SF.ReturnStatement( retExp );

			assignments = assignments.Add( ret );

			return assignments;
		}


		private SeparatedSyntaxList<ParameterSyntax> CreateAssignCheckBlock()
		{
			var paramList = new SeparatedSyntaxList<ParameterSyntax>();

			foreach( var f in m_fields )
			{
				var param = SF.Parameter( ModifyToken( "{0}Opt", f.Key.Identifier ) )
					.WithType( f.Value );

				param = SU.Optional( param );

				paramList = paramList.Add( param );
			}

			return paramList;
		}

		private SyntaxList<MemberDeclarationSyntax> CreateWithFunctions()
		{

			
			var returnType = SU.ClassNameWithGenerics(m_class);



			var retType = SF.IdentifierName( returnType );

			var withFn = SF.MethodDeclaration( retType, "with" );


			var paramList = CreateAssignCheckBlock();


			withFn = withFn.WithParameterList( SF.ParameterList( paramList ) );

			var block = SF.Block();

			block = block.WithStatements( CreateAssignments( "", "m_version + 1" ) );

			withFn = withFn.WithBody( block )
				.WithModifiers( SyntaxTokenList.Create( SF.Token( SyntaxKind.PublicKeyword ) ) );

			var list = new SyntaxList<MemberDeclarationSyntax>();

			list = list.Add( withFn );

			return list;
		}

		private SyntaxList<MemberDeclarationSyntax> CreateCreateFunctions()
		{
			var returnType = SU.ClassNameWithGenerics(m_class);

			var retType = SF.IdentifierName( returnType );

			var withFn = SF.MethodDeclaration( retType, "create" )
			.WithModifiers( SyntaxTokenList.Create( SF.Token( SyntaxKind.PublicKeyword ) ).Add( SF.Token( SyntaxKind.StaticKeyword ) ) );


			var paramList = CreateAssignCheckBlock();


			withFn = withFn.WithParameterList( SF.ParameterList( paramList ) );

			var block = SF.Block();

			block = block.WithStatements( CreateAssignments( "def.", "0" ) );

			withFn = withFn.WithBody( block );

			var list = new SyntaxList<MemberDeclarationSyntax>();

			list = list.Add( withFn );

			return list;
		}




		private SyntaxList<MemberDeclarationSyntax> CreateProtectedConstructors()
		{
			var list = new SyntaxList<MemberDeclarationSyntax>();

			//We implicitly have version
			//if( m_fields.Count > 0 )
			{
				BaseMethodDeclarationSyntax cons = SF.ConstructorDeclaration( m_class.Identifier );

				cons = SU.AddKeyword( cons, SyntaxKind.ProtectedKeyword );

				var paramList = new SeparatedSyntaxList<ParameterSyntax>();

				{
					var param = SF.Parameter( SF.Identifier( "version" ) )
						.WithType( SF.IdentifierName( "ulong" ) );

					paramList = paramList.Add( param );
				}


				foreach( var f in m_fields )
				{
					var param = SF.Parameter( f.Key.Identifier )
						.WithType( f.Value );

					paramList = paramList.Add( param );
				}



				cons = cons.WithParameterList( SF.ParameterList( paramList ) );

				var block = SF.Block();

				var assignments = new SyntaxList<StatementSyntax>();

				foreach( var f in m_fields )
				{
					var statement = SF.ParseStatement( $"this.{f.Key.Identifier} = {f.Key.Identifier};" );

					assignments = assignments.Add( statement );
				}

				block = block.WithStatements( assignments );

				cons = cons.WithBody( block );


				list = list.Add( cons );
			}


			{
				BaseMethodDeclarationSyntax cons = SF.ConstructorDeclaration( m_class.Identifier );

				cons = SU.AddKeyword( cons, SyntaxKind.ProtectedKeyword );

				var block = SF.Block();

				cons = cons.WithBody( block );

				list = list.Add( cons );
			}


			return list;
		}

		private SyntaxList<MemberDeclarationSyntax> CreateDefault()
		{
			var list = new SyntaxList<MemberDeclarationSyntax>();

			//var st = SF.ParseStatement( $"static public readonly {m_class.Identifier} def = new {m_class.Identifier};" );
			var newClass = SF.ParseExpression( $"new {SU.ClassNameWithGenerics(m_class)}()" );

			var declarator = SF.VariableDeclarator( "def" )
				.WithInitializer( SF.EqualsValueClause( newClass ) );

			var decl = SF.VariableDeclaration( SF.IdentifierName( SU.ClassNameWithGenerics(m_class) ), SF.SingletonSeparatedList( declarator ) );

			var keywords = SyntaxTokenList.Create( SF.Token( SyntaxKind.PublicKeyword ) )
				.Add( SF.Token( SyntaxKind.StaticKeyword ) )
				.Add( SF.Token( SyntaxKind.ReadOnlyKeyword ) );

			var field = SF.FieldDeclaration( decl )
				.WithModifiers( keywords );

			list = list.Add( field );

			return list;
		}


		private SyntaxList<MemberDeclarationSyntax> CreateVersion()
		{
			var list = new SyntaxList<MemberDeclarationSyntax>();

			/*
			var versionField = $"private unsigned long m_version = 0";

			var versionAcc = $"public unsigned long Version => m_version";

			var versionFieldParsed = SF.ParseStatement( versionField );

			var versionAccParsed = SF.ParseStatement( versionAcc );
			*/

			var versionField = SU.Field( "m_version", "ulong", SF.ParseExpression( "0" ), SyntaxKind.ProtectedKeyword );

			var versionAcc = SF.PropertyDeclaration(SF.IdentifierName("ulong"), "Version");


			versionAcc = versionAcc.WithExpressionBody( SF.ArrowExpressionClause( SF.ParseExpression( "m_version" ) ) )
				.WithSemicolonToken( SF.Token( SyntaxKind.SemicolonToken ) )
				.WithModifiers( new SyntaxTokenList( SF.Token( SyntaxKind.PublicKeyword ) ) );

			list = list.Add( versionField );
			list = list.Add( versionAcc );

			return list;
		}



	}




}
