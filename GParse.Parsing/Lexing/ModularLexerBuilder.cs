﻿using System;
using System.Text.RegularExpressions;
using GParse.Common;
using GParse.Common.IO;
using GParse.Common.Lexing;
using GParse.Parsing.Abstractions.Lexing;
using GParse.Parsing.Lexing.Modules;

namespace GParse.Parsing.Lexing
{
    /// <summary>
    /// Defines a <see cref="ILexer{TokenTypeT}" /> builder
    /// </summary>
    /// <typeparam name="TokenTypeT"></typeparam>
    public class ModularLexerBuilder<TokenTypeT> : ILexerBuilder<TokenTypeT>
    {
        /// <summary>
        /// The module tree
        /// </summary>
        protected readonly LexerModuleTree<TokenTypeT> Modules = new LexerModuleTree<TokenTypeT> ( );

        /// <summary>
        /// Adds a module to the lexer (affects existing instances)
        /// </summary>
        /// <param name="module"></param>
        public virtual void AddModule ( ILexerModule<TokenTypeT> module ) => this.Modules.AddChild ( module );

        /// <summary>
        /// Removes an module from the lexer (affects existing instances)
        /// </summary>
        /// <param name="module"></param>
        public virtual void RemoveModule ( ILexerModule<TokenTypeT> module ) => this.Modules.RemoveChild ( module );

        #region AddLiteral

        /// <summary>
        /// Defines a token as a literal string
        /// </summary>
        /// <param name="ID">The ID of the token</param>
        /// <param name="type">The type of the token</param>
        /// <param name="raw">The raw value of the token</param>
        public virtual void AddLiteral ( String ID, TokenTypeT type, String raw ) =>
            this.AddModule ( new LiteralLexerModule<TokenTypeT> ( ID, type, raw ) );

        /// <summary>
        /// Defines a token as a literal string
        /// </summary>
        /// <param name="ID">The ID of the token</param>
        /// <param name="type">The type of the token</param>
        /// <param name="raw">The raw value of the token</param>
        /// <param name="isTrivia">
        /// Whether this token is considered trivia (will not show
        /// up in the enumerated token sequence but inside
        /// <see cref="Token{TokenTypeT}.Trivia" /> instead)
        /// </param>
        public virtual void AddLiteral ( String ID, TokenTypeT type, String raw, Boolean isTrivia ) =>
            this.AddModule ( new LiteralLexerModule<TokenTypeT> ( ID, type, raw, isTrivia ) );

        /// <summary>
        /// Defines a token as a literal string
        /// </summary>
        /// <param name="ID">The ID of the token</param>
        /// <param name="type">The type of the token</param>
        /// <param name="raw">The raw value of the token</param>
        /// <param name="value">The value of this token</param>
        public virtual void AddLiteral ( String ID, TokenTypeT type, String raw, Object value ) =>
            this.AddModule ( new LiteralLexerModule<TokenTypeT> ( ID, type, raw, value ) );

        /// <summary>
        /// Defines a token as a literal string
        /// </summary>
        /// <param name="ID">The ID of the token</param>
        /// <param name="type">The type of the token</param>
        /// <param name="raw">The raw value of the token</param>
        /// <param name="value">The value of this token</param>
        /// <param name="isTrivia">
        /// Whether this token is considered trivia (will not show
        /// up in the enumerated token sequence but inside
        /// <see cref="Token{TokenTypeT}.Trivia" /> instead)
        /// </param>
        public virtual void AddLiteral ( String ID, TokenTypeT type, String raw, Object value, Boolean isTrivia ) =>
            this.AddModule ( new LiteralLexerModule<TokenTypeT> ( ID, type, raw, value, isTrivia ) );

        #endregion AddLiteral

        #region AddRegex

        /// <summary>
        /// Defines a token as a regex pattern
        /// </summary>
        /// <param name="ID">The ID of the token</param>
        /// <param name="type">The type of the token</param>
        /// <param name="regex">
        /// The pattern that will match the raw token value
        /// </param>
        /// <param name="prefix">
        /// The constant prefix of the regex expression (if any)
        /// </param>
        /// <param name="converter">
        /// The function to convert the raw value into a desired type
        /// </param>
        /// <param name="isTrivia">
        /// Whether this token is considered trivia (will not show
        /// up in the enumerated token sequence but inside
        /// <see cref="Token{TokenTypeT}.Trivia" /> instead)
        /// </param>
        public virtual void AddRegex ( String ID, TokenTypeT type, String regex, String prefix = null, Func<Match, Object> converter = null, Boolean isTrivia = false ) =>
            this.AddModule ( new RegexLexerModule<TokenTypeT> ( ID, type, regex, prefix, converter, isTrivia ) );

        #endregion AddRegex

        /// <summary>
        /// Creates a lexer that will enumerate the tokens in <paramref name="input" />
        /// </summary>
        /// <param name="input">The string input to be tokenized</param>
        /// <param name="diagnosticEmitter"></param>
        /// <returns></returns>
        public virtual ILexer<TokenTypeT> BuildLexer ( String input, IProgress<Diagnostic> diagnosticEmitter ) => this.BuildLexer ( new SourceCodeReader ( input ), diagnosticEmitter );

        /// <summary>
        /// Creates a lexer that will enumerate the tokens in <paramref name="reader" />
        /// </summary>
        /// <param name="reader">
        /// The reader of the input to be tokenized
        /// </param>
        /// <param name="diagnosticEmitter"></param>
        /// <returns></returns>
        public virtual ILexer<TokenTypeT> BuildLexer ( SourceCodeReader reader, IProgress<Diagnostic> diagnosticEmitter ) => new ModularLexer<TokenTypeT> ( this.Modules, reader, diagnosticEmitter );
    }
}
