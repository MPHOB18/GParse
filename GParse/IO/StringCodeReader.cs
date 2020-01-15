﻿using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GParse.IO
{
    /// <summary>
    /// A source code reader
    /// </summary>
    public class StringCodeReader : ICodeReader
    {
        /// <summary>
        /// A cache of compiled regex expressions
        /// </summary>
        private static readonly ConcurrentDictionary<String, Regex> _regexCache = new ConcurrentDictionary<String, Regex> ( );

        /// <summary>
        /// The string containing the code being read
        /// </summary>
        private readonly String _code;

        #region Location Management

        /// <inheritdoc/>
        public Int32 Line { get; private set; }

        /// <inheritdoc/>
        public Int32 Column { get; private set; }

        /// <inheritdoc/>
        public Int32 Position { get; private set; }

        /// <inheritdoc/>
        public SourceLocation Location => new SourceLocation ( this.Line, this.Column, this.Position );

        #endregion Location Management

        /// <inheritdoc/>
        public Int32 Length => this._code.Length;

        /// <inheritdoc/>
        public StringCodeReader ( String str )
        {
            this._code = str ?? throw new ArgumentNullException ( nameof ( str ) );
            this.Position = 0;
            this.Line = 1;
            this.Column = 1;
        }

        /// <inheritdoc/>
        public void Advance ( Int32 offset )
        {
            if ( offset < 0 )
                throw new ArgumentOutOfRangeException ( nameof ( offset ), "The offset must be positive." );
            if ( offset > this.Length - this.Position )
                throw new ArgumentOutOfRangeException ( nameof ( offset ), "Offset is too big." );

            var code = this._code;
            var lines = 0;
            var column = this.Column;
            var lastIdx = this.Position + offset - 1;
            for ( var i = this.Position; i <= lastIdx; i++ )
            {
                if ( code[i] == '\n' )
                {
                    lines++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
            this.Position += offset;
            this.Line += lines;
            this.Column = column;
        }

        #region Non-mutable Operations

        #region FindOffset

        /// <inheritdoc/>
        public Int32 FindOffset ( Char ch )
        {
            // Skip the IndexOf call if we're already at the end of the string
            if ( this.Position == this.Length )
                return -1;

            // We get a slice (span) of the string from the current position until the end of it and
            // then return the result of IndexOf because the result is supposed to be relative to
            // our current position
            ReadOnlySpan<Char> span = this._code.AsSpan ( this.Position );
            return span.IndexOf ( ch );
        }

        /// <inheritdoc/>
        public Int32 FindOffset ( String str )
        {
            if ( String.IsNullOrEmpty ( str ) )
                throw new ArgumentException ( "The string must not be null or empty.", nameof ( str ) );

            // Skip the IndexOf call if we're already at the end of the string
            if ( this.Position == this.Length )
                return -1;

            // We get a slice (span) of the string from the current position until the end of it and
            // then return the result of IndexOf because the result is supposed to be relative to
            // our current position
            ReadOnlySpan<Char> span = this._code.AsSpan ( this.Position );
            return span.IndexOf ( str, StringComparison.Ordinal );
        }

        /// <inheritdoc/>
        public Int32 FindOffset ( Predicate<Char> predicate )
        {
            if ( predicate == null )
                throw new ArgumentNullException ( nameof ( predicate ) );
            if ( this.Position == this.Length )
                return -1;

            ReadOnlySpan<Char> span = this._code.AsSpan ( this.Position );
            for ( var i = 0; i < span.Length; i++ )
            {
                if ( predicate ( span[i] ) )
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion FindOffset

        #region IsNext

        /// <inheritdoc/>
        public Boolean IsNext ( Char ch ) =>
            this.Position != this.Length && this._code[this.Position] == ch;

        /// <inheritdoc/>
        public Boolean IsNext ( String str )
        {
            var len = str.Length;
            if ( len > this.Length - this.Position )
                return false;

            ReadOnlySpan<Char> span = this._code.AsSpan ( this.Position );
            return span.StartsWith ( str, StringComparison.Ordinal );
        }

        /// <inheritdoc/>
        public Boolean IsNext ( ReadOnlySpan<Char> span )
        {
            var len = span.Length;
            if ( len > this.Length - this.Position )
                return false;

            ReadOnlySpan<Char> code = this._code.AsSpan ( this.Position );
            return code.StartsWith ( span, StringComparison.Ordinal );
        }

        #endregion IsNext

        #region Peek

        /// <inheritdoc/>
        public Char? Peek ( )
        {
            if ( this.Position == this.Length )
                return null;

            return this._code[this.Position];
        }

        /// <inheritdoc/>
        public Char? Peek ( Int32 offset )
        {
            if ( offset < 0 )
                throw new ArgumentOutOfRangeException ( nameof ( offset ), "The offset must be positive." );
            if ( offset >= this.Length - this.Position )
                return null;

            return this._code[this.Position + offset];
        }

        #endregion Peek

        #region PeekRegex

        /// <inheritdoc/>
        public Match PeekRegex ( String expression )
        {
            if ( !_regexCache.TryGetValue ( expression, out Regex regex ) )
            {
                regex = new Regex ( "\\G" + expression, RegexOptions.Compiled );
                _regexCache[expression] = regex;
            }

            return regex.Match ( this._code, this.Position );
        }

        /// <inheritdoc/>
        public Match PeekRegex ( Regex regex )
        {
            Match match = regex.Match ( this._code, this.Position );
            if ( match.Success && match.Index != this.Position )
                throw new ArgumentException ( "The regular expression being used does not contain the '\\G' modifier at the start. The matched result does not start at the reader's current location.", nameof ( regex ) );
            return match;
        }

        #endregion PeekRegex

        #region PeekString

        /// <inheritdoc/>
        public String PeekString ( Int32 length )
        {
            if ( length < 0 )
                throw new ArgumentOutOfRangeException ( nameof ( length ), "Length must be positive." );
            if ( length > this.Length - this.Position )
                return null;

            return this._code.Substring ( this.Position, length );
        }

        #endregion PeekString

        #region PeekSpan

        /// <summary>
        /// Reads a span of the provided length without advancing the stream.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public ReadOnlySpan<Char> PeekSpan ( Int32 length )
        {
            if ( length < 0 )
                throw new ArgumentOutOfRangeException ( nameof ( length ), "Length must be positive." );
            if ( length > this.Length - this.Position )
                return null;

            return this._code.AsSpan ( this.Position, length );
        }

        #endregion PeekString

        #endregion Non-mutable Operations

        #region Mutable Operations

        #region Read

        /// <inheritdoc/>
        public Char? Read ( )
        {
            if ( this.Position == this.Length )
                return null;

            // Maybe use try-finally here?
            var @return = this._code[this.Position];
            this.Advance ( 1 );
            return @return;
        }

        /// <inheritdoc/>
        public Char? Read ( Int32 offset )
        {
            if ( offset < 0 )
                throw new ArgumentOutOfRangeException ( nameof ( offset ), "The offset must be positive." );
            if ( offset == 0 )
                return this.Read ( );
            if ( offset >= this.Length - this.Position )
                return null;

            // Maybe use try-finally here?
            var @return = this._code[this.Position + offset];
            this.Advance ( offset + 1 );
            return @return;
        }

        #endregion Read

        #region ReadLine

        /// <inheritdoc/>
        public String ReadLine ( )
        {
            // Expect CR + LF
            var crLfOffset = this.FindOffset ( "\r\n" );
            if ( crLfOffset > -1 )
            {
                var line = this.ReadString ( crLfOffset );
                this.Advance ( 2 );
                return line;
            }

            // Fallback to LF if no CR + LF
            var lfOffset = this.FindOffset ( '\n' );
            if ( lfOffset > -1 )
            {
                var line = this.ReadString ( lfOffset );
                this.Advance ( 1 );
                return line;
            }

            // Fallback to CR if no CR + LF nor LF
            var crOffset = this.FindOffset ( '\r' );
            if ( crOffset > -1 )
            {
                var line = this.ReadString ( crOffset );
                this.Advance ( 1 );
                return line;
            }

            // Fallback to EOF if no CR+LF, CR or LF
            return this.ReadToEnd ( );
        }

        #endregion ReadLine

        #region ReadSpanLine

        /// <summary>
        /// Reads a line from the stream. The returned span does not contain the end-of-line character.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The following are considered line endings:
        /// <list type="bullet">
        /// <item>CR + LF (\r\n)</item>
        /// <item>LF (\n)</item>
        /// <item>CR (\r)</item>
        /// <item><see cref="Environment.NewLine"/></item>
        /// <item>EOF</item>
        /// </list>
        /// </remarks>
        public ReadOnlySpan<Char> ReadSpanLine ( )
        {
            // Expect CR + LF
            var crLfOffset = this.FindOffset ( "\r\n" );
            if ( crLfOffset > -1 )
            {
                ReadOnlySpan<Char> line = this.ReadSpan ( crLfOffset );
                this.Advance ( 2 );
                return line;
            }

            // Fallback to LF if no CR + LF
            var lfOffset = this.FindOffset ( '\n' );
            if ( lfOffset > -1 )
            {
                ReadOnlySpan<Char> line = this.ReadSpan ( lfOffset );
                this.Advance ( 1 );
                return line;
            }

            // Fallback to CR if no CR + LF nor LF
            var crOffset = this.FindOffset ( '\r' );
            if ( crOffset > -1 )
            {
                ReadOnlySpan<Char> line = this.ReadSpan ( crOffset );
                this.Advance ( 1 );
                return line;
            }

            // Fallback to EOF if no CR+LF, CR or LF
            return this.ReadSpanToEnd ( );
        }

        #endregion ReadSpanLine

        #region ReadString

        /// <inheritdoc/>
        public String ReadString ( Int32 length )
        {
            if ( length < 0 )
                throw new ArgumentOutOfRangeException ( nameof ( length ), "Length must be positive." );
            if ( length == 0 )
                return String.Empty;
            if ( length > this.Length - this.Position )
                return null;

            // Maybe use try-finally here?
            var @return = this._code.Substring ( this.Position, length );
            this.Advance ( length );
            return @return;
        }

        #endregion ReadString

        #region ReadStringUntil

        /// <inheritdoc/>
        public String ReadStringUntil ( Char delim )
        {
            var length = this.FindOffset ( delim );
            if ( length > -1 )
                return this.ReadString ( length );
            else
                return this.ReadToEnd ( );
        }

        /// <inheritdoc/>
        public String ReadStringUntil ( String delim )
        {
            var length = this.FindOffset ( delim );
            if ( length > -1 )
                return this.ReadString ( length );
            else
                return this.ReadToEnd ( );
        }

        /// <inheritdoc/>
        public String ReadStringUntil ( Predicate<Char> filter )
        {
            if ( filter == null )
                throw new ArgumentNullException ( nameof ( filter ) );

            var length = this.FindOffset ( filter );
            if ( length > -1 )
                return this.ReadString ( length );
            else
                return this.ReadToEnd ( );
        }

        #endregion ReadStringUntil

        #region ReadStringWhile

        /// <inheritdoc/>
        public String ReadStringWhile ( Predicate<Char> filter )
        {
            if ( filter == null )
                throw new ArgumentNullException ( nameof ( filter ) );

            var length = this.FindOffset ( v => !filter ( v ) );
            if ( length > -1 )
                return this.ReadString ( length );
            else
                return this.ReadToEnd ( );
        }

        #endregion ReadStringWhile

        #region ReadSpan

        /// <summary>
        /// Reads a span of the given length from the stream.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public ReadOnlySpan<Char> ReadSpan ( Int32 length )
        {
            if ( length < 0 )
                throw new ArgumentOutOfRangeException ( nameof ( length ), "Length must be positive." );
            if ( length == 0 )
                return String.Empty;
            if ( length > this.Length - this.Position )
                return null;

            // Maybe use try-finally here?
            ReadOnlySpan<Char> @return = this._code.AsSpan ( this.Position, length );
            this.Advance ( length );
            return @return;
        }

        #endregion ReadSpan

        #region ReadSpanUntil

        /// <summary>
        /// Reads the contents from the stream until the provided <paramref name="delim"/> is found
        /// or the end of the stream is hit.
        /// </summary>
        /// <param name="delim"></param>
        /// <returns></returns>
        public ReadOnlySpan<Char> ReadSpanUntil ( Char delim )
        {
            var length = this.FindOffset ( delim );
            if ( length > -1 )
                return this.ReadSpan ( length );
            else
                return this.ReadSpanToEnd ( );
        }

        /// <summary>
        /// Reads the contents from the stream until the provided <paramref name="delim"/> is found
        /// or the end of the stream is hit.
        /// </summary>
        /// <param name="delim"></param>
        /// <returns></returns>
        public ReadOnlySpan<Char> ReadSpanUntil ( String delim )
        {
            var length = this.FindOffset ( delim );
            if ( length > -1 )
                return this.ReadSpan ( length );
            else
                return this.ReadSpanToEnd ( );
        }

        /// <summary>
        /// Reads the contents from the stream until a character passes the provided <paramref
        /// name="filter"/> or the end of the stream is hit.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public ReadOnlySpan<Char> ReadSpanUntil ( Predicate<Char> filter )
        {
            if ( filter == null )
                throw new ArgumentNullException ( nameof ( filter ) );

            var length = this.FindOffset ( filter );
            if ( length > -1 )
                return this.ReadSpan ( length );
            else
                return this.ReadSpanToEnd ( );
        }

        #endregion ReadSpanUntil

        #region ReadSpanWhile

        /// <summary>
        /// Reads the contents from the stream while the characters pass the provided <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public ReadOnlySpan<Char> ReadSpanWhile ( Predicate<Char> filter )
        {
            if ( filter == null )
                throw new ArgumentNullException ( nameof ( filter ) );

            var length = this.FindOffset ( v => !filter ( v ) );
            if ( length > -1 )
                return this.ReadSpan ( length );
            else
                return this.ReadSpanToEnd ( );
        }

        #endregion ReadSpanWhile

        #region ReadToEnd

        /// <inheritdoc/>
        public String ReadToEnd ( )
        {
            var ret = this._code.Substring ( this.Position );
            this.Position = this.Length;
            return ret;
        }

        #endregion ReadToEnd

        #region ReadSpanToEnd

        /// <summary>
        /// Reads the contents from the stream until the end of the stream.
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<Char> ReadSpanToEnd ( )
        {
            ReadOnlySpan<Char> ret = this._code.AsSpan ( this.Position );
            this.Position = this.Length;
            return ret;
        }

        #endregion ReadSpanToEnd

        #region MatchRegex

        /// <inheritdoc/>
        public Match MatchRegex ( String expression )
        {
            if ( !_regexCache.TryGetValue ( expression, out Regex regex ) )
            {
                regex = new Regex ( "\\G" + expression, RegexOptions.Compiled );
                _regexCache[expression] = regex;
            }

            Match match = regex.Match ( this._code, this.Position );
            if ( match.Success )
                this.Advance ( match.Length );
            return match;
        }

        /// <inheritdoc/>
        public Match MatchRegex ( Regex regex )
        {
            Match match = regex.Match ( this._code, this.Position );
            if ( match.Success )
            {
                if ( match.Index != this.Position )
                    throw new ArgumentException ( "The regular expression being used does not contain the '\\G' modifier at the start. The matched result does not start at the reader's current location.", nameof ( regex ) );
                this.Advance ( match.Length );
            }
            return match;
        }

        #endregion MatchRegex

        #endregion Mutable Operations

        #region Position Manipulation

        /// <inheritdoc/>
        public void Reset ( )
        {
            this.Line = 1;
            this.Column = 1;
            this.Position = 0;
        }

        /// <inheritdoc/>
        public void Restore ( SourceLocation location )
        {
            if ( location.Line < 0 || location.Column < 0 || location.Byte < 0 )
                throw new Exception ( "Invalid rewind position." );

            this.Line = location.Line;
            this.Column = location.Column;
            this.Position = location.Byte;
        }

        #endregion Position Manipulation
    }
}