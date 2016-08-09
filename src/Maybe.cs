using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace erecruit.Utils
{
	[DebuggerStepThrough]
	[ContractClass( typeof( Contracts.MaybeContracts<> ) )]
	public abstract class Maybe<T>
	{
		public abstract T Value { get; }
		public static explicit operator T( Maybe<T> m ) { return m.Value; }
		public static implicit operator Maybe<T>( T t ) { return t == null ? Nothing : From( t ); } // TODO: [fs] Remove this. Dangerous.

		public abstract Maybe.Kind Kind { get; }
		public abstract Maybe.Error Error { get; }
		public bool HasValue { get { return Kind == Maybe.Kind.Value; } }

		public static Maybe<T> From( T v ) { return new Just( v ); }
		public static Maybe<T> From( Exception err ) { return new _Error( new Maybe.Error( err ) ); }
		public static Maybe<T> From( Maybe.Error err ) { return new _Error( err ); }

		static readonly Maybe<T> _nothing = new _Nothing();
		public static Maybe<T> Nothing { get { return _nothing; } }

		public abstract Maybe<V> Then<V>( Func<T, Maybe<V>> fn );
		protected abstract Maybe<V> ChangeType<V>();

		[EditorBrowsable( EditorBrowsableState.Never )] // LINQ operators should only be used by the LINQ-syntax-desugared code. Other code use normal operators (above).
		public Maybe<R> SelectMany<V, R>( Func<T, Maybe<V>> fn, Func<T, V, R> result ) {
			Contract.Requires( fn != null );
			Contract.Requires( result != null );
			Contract.Ensures( Contract.Result<Maybe<R>>() != null );
			return this.Then( new SelectManyHelper<V,R>( fn, result ).CallFn );
		}

		[DebuggerStepThrough]
		class Just : Maybe<T>
		{
			private readonly T _value;
			public override T Value { get { return _value; } }
			public override Maybe.Kind Kind { get { return Maybe.Kind.Value; } }
			public override Maybe.Error Error { get { return null; } }
			public Just( T value ) { this._value = value; }

			protected override Maybe<V> ChangeType<V>() { throw new NotSupportedException(); }
			public override Maybe<V> Then<V>( Func<T, Maybe<V>> fn )
			{
				try 
				{
					var v = fn( this._value );
					if ( v == null ) return Maybe.Fail<V>( "The mapping function passed to Maybe.SelectMany returned a null Maybe object. Maybe objects should never be null." );
					return v;
				}
				catch( Exception ex )
				{
					return Maybe<V>.From( ex );
				}
			}

			public override string ToString() {
				return Convert.ToString( Value );
			}
		}

		[DebuggerStepThrough]
		class _Error : Maybe<T>
		{
			private readonly Maybe.Error _error;
			public override T Value { get { _error.Throw(); return default( T ); } }
			public override Maybe.Kind Kind { get { return Maybe.Kind.Error; } }
			public override Maybe.Error Error { get { return _error; } }
			protected override Maybe<V> ChangeType<V>() { return Maybe<V>.From( this._error ); }
			public override Maybe<V> Then<V>( Func<T, Maybe<V>> fn ) { return Maybe<V>.From( _error ); }
			public _Error( Maybe.Error err ) { this._error = err; }

			public override string ToString() {
				return "ERROR: " + Error;
			}
		}

		[DebuggerStepThrough]
		class _Nothing : Maybe<T>
		{
			public override T Value { get { throw new Maybe.NoValueException(); } }
			public override Maybe.Kind Kind { get { return Maybe.Kind.Nothing; } }
			public override Maybe.Error Error { get { return null; } }
			protected override Maybe<V> ChangeType<V>() { return Maybe<V>.Nothing; }
			public override Maybe<V> Then<V>( Func<T, Maybe<V>> fn ) { return Maybe<V>.Nothing; }

			public override string ToString() {
				return "<nothing>";
			}
		}


		// This class is here solely for the purpose of applying [DebuggerStepThrough] attribute to its methods.
		// Apparently, the C# compiler team is not up to the challenge: http://connect.microsoft.com/VisualStudio/feedback/details/336367/debuggerstepthroughattribute-is-ignored-when-stepping-into-lambda-expressions-from-a-different-method
		// If not for this reason, these methods could be implemented as lambdas in place
		[DebuggerStepThrough]
		class SelectManyHelper<V, R>
		{
			private readonly Func<T, Maybe<V>> _fn;
			private readonly Func<T, V, R> _result;
			public SelectManyHelper( Func<T, Maybe<V>> fn, Func<T, V, R> result ) {
				Contract.Requires( fn != null );
				Contract.Requires( result != null );
				_fn = fn; _result = result;
			}

			public Maybe<R> CallFn( T t ) {
				return (_fn( t ) ?? Maybe.Nothing<V>()).Then( new InnerHelper( _result, t ).CallResult );
			}

			[DebuggerStepThrough]
			class InnerHelper
			{
				private readonly T _t;
				private readonly Func<T, V, R> _result;

				public InnerHelper( Func<T, V, R> result, T t ) {
					Contract.Requires( result != null );
					_result = result;
					_t = t;
				}

				public Maybe<R> CallResult( V v ) {
					return Maybe.From( _result( _t, v ) );
				}
			}
		}
	}

	// It's named without capitalization in order to avoid conflict with System.Web.UI.WebControls.Unit,
	// and I couldn't come up with a better name
	public sealed class unit
	{
		public static readonly unit Default = new unit();
		private unit() { }
	}

	[DebuggerStepThrough]
	public static partial class Maybe
	{
		public enum Kind { Nothing, Value, Error };

		public static readonly Maybe<unit> Unit = unit.Default.AsMaybe();

		#region Static methods
		public static Maybe<T> From<T>( T v ) {
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return Maybe<T>.From( v );
		}

		public static Maybe<T> Nothing<T>() {
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return Maybe<T>.Nothing; 
		}

		public static Maybe<T> Throw<T>( Exception err, T valueExampleJustForTypeInferrence = default( T ) ) {
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return Maybe<T>.From( err ); 
		}

		public static Maybe<T> Fail<T>( Error err, T valueExampleJustForTypeInferrence = default( T ) ) {
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return Maybe<T>.From( err ); 
		}

		public static Maybe<T> Fail<T>( string message, T valueExampleJustForTypeInferrence = default( T ) ) {
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return Maybe<T>.From( new Error( message ) ); 
		}

		public static Maybe<unit> FailWhen( bool condition, string messageFormat, params object[] formatArgs ) {
			Contract.Requires( messageFormat != null );
			Contract.Ensures( Contract.Result<Maybe<unit>>() != null );
			return condition ? Maybe.Fail<unit>( string.Format( messageFormat, formatArgs ) ) : Maybe.Unit; 
		}

		public static Maybe<unit> FailWhen(bool condition, Func<string> message)
		{
			Contract.Requires( message != null );
			Contract.Ensures( Contract.Result<Maybe<unit>>() != null );
			return condition ? Maybe.Fail<unit>( message() ) : Maybe.Unit; 
		}

		/// <summary>
		/// If the given Maybe computation ended in a failure, throws an exception.
		/// Otherwise, does nothing.
		/// </summary>
		public static void CrashIfError<T>( this Maybe<T> m ) {
			Contract.Requires( m != null );
			if ( m.Kind == Kind.Error ) m.Error.Throw();
		}

		public static Maybe<unit> Do( Action a ) {
			Contract.Requires( a != null );
			Contract.Ensures( Contract.Result<Maybe<unit>>() != null );
			return Eval( a );
		}

		public static Maybe<unit> DoWhen( bool condition, Action a ) {
			Contract.Requires( a != null );
			Contract.Ensures( Contract.Result<Maybe<unit>>() != null );
			return Eval( () => { if ( condition ) a(); } );
		}

		public static Maybe<T> Unwrap<T>( this Maybe<Maybe<T>> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Then( x => x );
		}

		public static Maybe<T> Unwrap<T>( this Maybe<Maybe<Maybe<T>>> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Then( x => x ).Then( x => x );
		}

		public static Maybe<unit> Holds( bool condition ) {
			Contract.Ensures( Contract.Result<Maybe<unit>>() != null );
			return condition ? unit.Default.AsMaybe() : Nothing<unit>();
		}

		public static Maybe<T> If<T>( bool condition, Func<Maybe<T>> then, Func<Maybe<T>> otherwise = null ) {
			Contract.Requires(then != null);
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return 
				condition ? then() : 
				otherwise != null ? otherwise()
				: Nothing<T>();
		}

		public static Maybe<T> Eval<T>( Func<T> f ) {
			Contract.Requires( f != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return 0.AsMaybe().Then( _ => f() );
		}

		public static Maybe<T> Eval<T>( Func<Maybe<T>> f ) {
			Contract.Requires( f != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return 0.AsMaybe().Then( _ => f() ?? Nothing<T>() );
		}

		public static Maybe<unit> Eval( Action a ) {
			Contract.Requires( a != null );
			Contract.Ensures( Contract.Result<Maybe<unit>>() != null );
			return 0.AsMaybe().Then( _ => { a(); return unit.Default; } );
		}
		#endregion


		#region Extension methods
		public static Maybe<V> Then<T, V>( this Maybe<T> source, Func<T, V> fn ) {
			Contract.Requires( source != null );
			Contract.Requires( fn != null );
			Contract.Ensures( Contract.Result<Maybe<V>>() != null );
			return source.Then( x => Maybe.From( fn( x ) ) );
		}

		public static Maybe<T> AsMaybe<T>( this T v ) {
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return Maybe<T>.From( v );
		}

		public static Maybe<T> AsMaybe<T>( this Nullable<T> v ) where T : struct {
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return v.HasValue ? Maybe<T>.From( v.Value ) : Maybe<T>.Nothing;
		}

		public static T ValueOrDefault<T>( this Maybe<T> source, T defaultValue = default(T) ) {
			Contract.Requires( source != null );
			return source.Kind == Kind.Value ? source.Value : defaultValue;
		}

		public static Maybe<T> When<T>( this Maybe<T> source, Func<T, bool> condition ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Then( x => condition( x ) ? Maybe.From( x ) : Maybe.Nothing<T>() );
		}

		public static Maybe<T> WhenError<T>( this Maybe<T> source, Func<Error, Maybe<T>> backup ) {
			Contract.Requires( source != null );
			Contract.Requires( backup != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			if ( source.Kind != Kind.Error ) return source; return backup( source.Error );
		}

		public static Maybe<T> WhenError<T>( this Maybe<T> source, Func<Error, T> backup ) {
			Contract.Requires( source != null );
			Contract.Requires( backup != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			if ( source.Kind != Kind.Error ) return source; return Maybe.Eval( (Func<T>)new CallHelper<Error, T>( source.Error, backup ).Call );
		}
		
		public static Maybe<T> WhenNothing<T>( this Maybe<T> source, Func<Maybe<T>> backup ) {
			Contract.Requires( source != null );
			Contract.Requires( backup != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			if ( source.Kind != Kind.Nothing ) return source; return backup();
		}

		public static Maybe<T> WhenNothing<T>( this Maybe<T> source, Func<T> backup ) {
			Contract.Requires( source != null );
			Contract.Requires( backup != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			if ( source.Kind != Kind.Nothing ) return source; return Maybe.Eval( backup );
		}

		public static Maybe<T> WhenNothingReturnDefault<T>( this Maybe<T> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.WhenNothing( (Func<T>)Helpers.ReturnDefault<T> );
		}

		public static Maybe<IEnumerable<T>> WhenNothingReturnEmptySeq<T>( this Maybe<IEnumerable<T>> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<IEnumerable<T>>>() != null );
			return source.WhenNothing( () => Enumerable.Empty<T>() );
		}

 		public static Maybe<IDictionary<T,U>> WhenNothingReturnEmptyDict<T, U>( this Maybe<IDictionary<T, U>> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<IDictionary<T, U>>>() != null );
			return source.WhenNothing( () => new Dictionary<T, U>() as IDictionary<T, U> );
		}

		public static Maybe<T> WhenNothingFail<T>( this Maybe<T> source, Func<Exception> createError ) {
			Contract.Requires( source != null );
			Contract.Requires( createError != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			if ( source.Kind == Kind.Nothing ) return Maybe.Throw<T>( createError() ); else return source;
		}

		public static Maybe<T> WhenNothingFail<T>( this Maybe<T> source, Func<Error> createError ) {
			Contract.Requires( source != null );
			Contract.Requires( createError != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			if ( source.Kind == Kind.Nothing ) return Maybe.Fail<T>( createError() ); else return source;
		}

		public static Maybe<T> WhenNothingFail<T>( this Maybe<T> source, string errorMessage ) {
			Contract.Requires( source != null );
			Contract.Requires( errorMessage != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.WhenNothingFail( () => new Error( errorMessage ) );
		}

		public static Maybe<T> WhenNothingFail<T>( this Maybe<T> source, string errorMessageFormat, params object[] args ) {
			Contract.Requires( source != null );
			Contract.Requires( errorMessageFormat != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.WhenNothingFail( () => new Error( string.Format( errorMessageFormat, args ) ) );
		}

		public static CastBuilder<T> Cast<T>( this Maybe<T> source ) {
			Contract.Requires( source != null );
			return new CastBuilder<T>( source );
		}

		public static Maybe<T> Do<T>( this Maybe<T> source, Action<T> a ) {
			Contract.Requires( source != null );
			Contract.Requires( a != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Then( new DoHelper<T>( a ).Wrap );
		}
		#endregion


		#region Parsing/conversions
		public static Maybe<T> MaybeDefined<T>( this T t ) where T : class { return t == null ? Maybe<T>.Nothing : Maybe<T>.From( t ); }
		public static Maybe<T> MaybeDefined<T>( this Maybe<T> t ) { return t ?? Maybe.Nothing<T>(); }
		public static Maybe<T> MaybeNotNull<T>( this T? t ) where T : struct { return t == null ? Maybe<T>.Nothing : Maybe<T>.From( t.Value ); }
		public static Maybe<T> MaybeEnum<T>( string value ) where T : struct {
			T enumType;
			return Maybe.If<T>( Enum.TryParse( value, out enumType ), () => enumType );
		}

		public static Maybe<V> MaybeValue<T, V>( this Dictionary<T, V> dict, T key ) {
			return (dict as IDictionary<T, V>).MaybeValue( key );
		}

		public static Maybe<V> MaybeValue<T, V>( this IDictionary<T, V> dict, T key ) {
			Contract.Requires( dict != null );
			Contract.Ensures( Contract.Result<Maybe<V>>() != null );
			V res;
			return dict.TryGetValue( key, out res ) ? res.AsMaybe() : Nothing<V>();
		}

		public static Maybe<V> MaybeValue<T, V>( this IReadOnlyDictionary<T, V> dict, T key ) {
			Contract.Requires( dict != null );
			Contract.Ensures( Contract.Result<Maybe<V>>() != null );
			V res;
			return dict.TryGetValue( key, out res ) ? res.AsMaybe() : Nothing<V>();
		}

		public static Maybe<T> MaybeFirst<T>( this IEnumerable<T> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Select( AsMaybe ).MaybeFirst();
		}

		public static Maybe<T> MaybeFirst<T>( this IEnumerable<T> source, Func<T, bool> predicate ) {
			Contract.Requires( source != null );
			Contract.Requires( predicate != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Select( AsMaybe ).MaybeFirst( predicate );
		}

		public static Maybe<T> MaybeFirst<T>( this IEnumerable<Maybe<T>> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.MaybeFirst( ( T _ ) => true );
		}

		public static Maybe<T> MaybeFirst<T>( this IEnumerable<Maybe<T>> source, Func<T, bool> predicate ) {
			Contract.Requires( source != null );
			Contract.Requires( predicate != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source
				.Where( x => x != null && x.Then( predicate ).ValueOrDefault() )
				.DefaultIfEmpty( Maybe.Nothing<T>() )
				.FirstOrDefault();
		}
		#endregion

		public static Maybe<T> LogErrors<T>( this Maybe<T> m, Action<object> log ) {
			Contract.Requires( m != null );
			Contract.Requires( log != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			if ( m.Kind == Kind.Error ) {
				if ( m.Error.Exception != null ) log( m.Error.Exception );
				else if ( m.Error.Messages != null ) {
					foreach ( var msg in m.Error.Messages ) log( msg );
				}
			}

			return m;
		}

		/// <summary>
		/// Turns a sequence of Maybes into a Maybe of sequence. See remarks.
		/// </summary>
		/// <remarks>
		/// If any of the Maybe's in the source sequence are error or exception,
		/// the resulting Maybe will be an AggregateException of all those exceptions or a concatenation of all those errors. Otherwise,
		/// the resulting Maybe will be a sequence of all Maybe's in the source sequence that have a value. If there are no elements
		/// of the source sequence that have a value, the result will be an empty sequence.
		/// </remarks>
		public static Maybe<IEnumerable<T>> Lift<T>( this IEnumerable<Maybe<T>> source ) {
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<IEnumerable<T>>>() != null );

			if ( !(source is IList<T>) ) source = source.Where( m => m != null ).ToList(); // Don't evaluate it more than once
			var exceptions = source.Where( m => m.Kind == Kind.Error && m.Error.Exception != null ).Select( m => m.Error.Exception ).ToList();
			var errorMsgs = source.Where( m => m.Kind == Kind.Error && m.Error.Exception == null ).SelectMany( m => m.Error.Messages?.DefaultIfEmpty() ).ToList();
			if ( exceptions.Any() || errorMsgs.Any() ) {
				return Maybe.Fail<IEnumerable<T>>( exceptions.Any()
					? new Error( new AggregateException( exceptions.Concat( errorMsgs.Select( m => new InvalidOperationException( m ) ) ) ) )
					: new Error( errorMsgs ) );
			}

			return source.Where( m => m.Kind == Kind.Value ).Select( m => m.Value ).AsMaybe();
		}

		[DebuggerStepThrough]
		public class Error
		{
			public Exception Exception { get; private set; }
			public string FirstMessage { get { return Messages.FirstOrDefault() ?? (Exception != null ? Exception.Message : null); } }
			public IEnumerable<string> Messages { get; private set; }
			public Error( Exception ex ) {
				this.Exception = ex;
				var a = ex as AggregateException;
				this.Messages = a == null
					? FlattenInners( ex ).Select( e => e.Message )
					: a.Flatten().InnerExceptions.SelectMany( FlattenInners ).Select( e => e.Message );
			}

			private IEnumerable<Exception> FlattenInners( System.Exception ex ) {
				while ( ex != null ) { yield return ex; ex = ex.InnerException; }
			}
			public Error( IEnumerable<string> msgs ) { this.Messages = msgs ?? Enumerable.Empty<string>(); }
			public Error( string msg ) : this( new[] { msg }.AsEnumerable() ) { }
			public Error( params string[] msgs ) : this( msgs?.AsEnumerable() ) { }

			public static Error Format( string formatString, params object[] args ) { return new Error( string.Format( formatString, args ) ); }

			public void Throw() {
				if ( this.Exception != null ) throw new InvalidOperationException( "An exception was thrown during a Maybe computation", this.Exception );
				throw new InvalidOperationException( string.Join( ", ", this.Messages ) );
			}

			public override string ToString() {
				return string.Join( Environment.NewLine, Messages );
			}
		}

		[DebuggerStepThrough]
		public struct CastBuilder<T>
		{
			private readonly Maybe<T> _source;
			public CastBuilder( Maybe<T> source ) { Contract.Requires( source != null ); _source = source; }
			public Maybe<U> As<U>() where U : T {
				Contract.Ensures( Contract.Result<Maybe<U>>() != null );
				return _source.Where( x => x == null || x is U ).Select( x => x == null ? default( U ) : (U)x );
			}
		}

		// See comment on MaybeLINQExtensions.SelectManyHelper below
		[DebuggerStepThrough]
		class DoHelper<T>
		{
			private readonly Action<T> _f;
			public DoHelper( Action<T> f ) { _f = f; }
			public Maybe<T> Wrap( T t ) { _f( t ); return Maybe.From( t ); }
		}

		// See comment on MaybeLINQExtensions.SelectManyHelper below
		[DebuggerStepThrough]
		class CallHelper<T, U>
		{
			private readonly T _arg;
			private readonly Func<T, U> _f;
			public CallHelper( T arg, Func<T, U> f ) { _arg = arg; _f = f; }
			public U Call() { return _f( _arg ); }
		}

		// See comment on MaybeLINQExtensions.SelectManyHelper below
		[DebuggerStepThrough]
		static class Helpers
		{
			public static T ReturnDefault<T>() { return default( T ); }
		}
	}

	[DebuggerStepThrough]
	public static class MaybeLINQExtensions 
	{
		[EditorBrowsable( EditorBrowsableState.Never )] // LINQ operators should only be used by the LINQ-syntax-desugared code. Other code use normal operators (above).
		public static Maybe<R> SelectMany<T, V, R>( this Maybe<T> source, Func<T, V> fn, Func<T, V, R> result ) {
			Contract.Requires( source != null );
			Contract.Requires( fn != null );
			Contract.Requires( result != null );
			Contract.Ensures( Contract.Result<Maybe<R>>() != null );
			return source.SelectMany( new SelectManyHelper<T, V>( fn ).Wrap, result );
		}

		[EditorBrowsable( EditorBrowsableState.Never )]
		public static Maybe<T> Where<T>( this Maybe<T> source, Func<T, bool> fn ) {
			Contract.Requires( source != null );
			Contract.Requires( fn != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Then( new WhereHelper<T>( fn ).Wrap ); 
		}

		[EditorBrowsable( EditorBrowsableState.Never )]
		public static Maybe<T> Where<T>( this Maybe<T> source, Func<T, Maybe<unit>> fn ) {
			Contract.Requires( source != null );
			Contract.Requires( fn != null );
			Contract.Ensures( Contract.Result<Maybe<T>>() != null );
			return source.Then( new WhereHoldsHelper<T>( source, fn ).Wrap ); 
		}

		[EditorBrowsable( EditorBrowsableState.Never )]
		public static Maybe<V> Select<T, V>( this Maybe<T> source, Func<T, V> fn ) { 
			Contract.Requires( source != null );
			Contract.Ensures( Contract.Result<Maybe<V>>() != null );
			return source.Then( fn ); 
		}

		// This class is here solely for the purpose of applying [DebuggerStepThrough] attribute to its methods.
		// Apparently, the C# compiler team is not up to the challenge: http://connect.microsoft.com/VisualStudio/feedback/details/336367/debuggerstepthroughattribute-is-ignored-when-stepping-into-lambda-expressions-from-a-different-method
		// If not for this reason, these method could be implemented as lambdas in place
		[DebuggerStepThrough]
		class SelectManyHelper<T,U>
		{
			private readonly Func<T, U> _f;
			public SelectManyHelper( Func<T,U> f ) { _f = f; }
			public Maybe<U> Wrap( T t ) { return _f( t ).AsMaybe(); }
			public static U IgnoreFirst( T t, U u ) { return u; }
		}

		// See comment on SelectManyHelper above
		[DebuggerStepThrough]
		class WhereHelper<T>
		{
			private readonly Func<T, bool> _f;
			public WhereHelper( Func<T, bool> f ) { _f = f; }
			public Maybe<T> Wrap( T t ) { return _f( t ) ? t.AsMaybe() : Maybe.Nothing<T>(); }
		}

		// See comment on SelectManyHelper above
		[DebuggerStepThrough]
		class WhereHoldsHelper<T>
		{
			private readonly Maybe<T> _source;
			private readonly Func<T, Maybe<unit>> _f;
			public WhereHoldsHelper( Maybe<T> source, Func<T, Maybe<unit>> f ) { _source = source; _f = f; }
			public Maybe<T> Wrap( T t ) {
				var condition = _f( t );
				return condition.HasValue ? _source : condition.Then( ReturnDefault );
			}
			T ReturnDefault( unit _ ) { return default( T ); }
		}
	}

	namespace Contracts
	{
		[ContractClassFor(typeof(Maybe<>))]
		abstract class MaybeContracts<T> : Maybe<T>
		{
			public override Maybe<V> Then<V>( Func<T, Maybe<V>> fn ) {
				Contract.Requires( fn != null );
				Contract.Ensures( Contract.Result<Maybe<V>>() != null );
				throw new NotImplementedException();
			}
		}
	}
}