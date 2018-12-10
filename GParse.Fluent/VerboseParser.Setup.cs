﻿using System;
using System.Collections.Generic;
using GParse.Fluent.Matchers;
using GParse.Fluent.Optimization;
using GParse.Fluent.Parsing;
using GParse.Fluent.Visitors;

namespace GParse.Fluent
{
    public abstract partial class FluentParser<NodeT>
    {
        /// <summary>
        /// Defines the interface of a node factory
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        public delegate NodeT NodeFactory ( String Name, MatchResult<NodeT> Result );

        private readonly Dictionary<String, BaseMatcher> Rules = new Dictionary<String, BaseMatcher> ( );
        private readonly Dictionary<String, NodeFactory> Factories = new Dictionary<String, NodeFactory> ( );
        private readonly NodeFactory MarkerNodeFactory;
        private readonly ExpressionParser ExpressionParser;
        internal String RootName;

        /// <summary>
        /// Initializes this class
        /// </summary>
        protected FluentParser ( NodeFactory markerNodeFactory )
        {
            this.MarkerNodeFactory = markerNodeFactory ?? throw new ArgumentNullException ( nameof ( markerNodeFactory ) );
            this.ExpressionParser = new ExpressionParser ( );
            this.Setup ( );
            if ( this.RootName == null )
                throw new InvalidOperationException ( "Can't initialize a parser without a root rule." );
            this.LengthCalculator = new MaximumMatchLengthCalculator<NodeT> ( this );
        }

        /// <summary>
        /// This function should set up the parser by calling the
        /// <see cref="Rule(String, BaseMatcher, NodeFactory)" />,
        /// <see cref="Factory(String, NodeFactory)" /> and
        /// <see cref="RootRule(String, String, NodeFactory)" /> functions
        /// </summary>
        protected abstract void Setup ( );

        /// <summary>
        /// Optimizes all rules in the parser
        /// </summary>
        protected void Optimize ( )
        {
            if ( this.RootName == null )
                throw new InvalidOperationException ( "Optimization can only be done at the end of setup." );
            var optimizer = new MatchTreeOptimizer ( );
            var optimized = new Dictionary<String, BaseMatcher> ( );

            foreach ( KeyValuePair<String, BaseMatcher> kv in this.Rules )
                optimized[kv.Key] = kv.Value.Accept ( optimizer );

            foreach ( KeyValuePair<String, BaseMatcher> kv in optimized )
                this.Rules[kv.Key] = kv.Value;
        }

        /// <summary>
        /// Creates a Matcher based on the pattern passed to it.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        protected BaseMatcher ParseMatcher ( String pattern )
            => this.ExpressionParser.Parse ( pattern );

        /// <summary>
        /// Creates a matcher based on the filter passed to it
        /// </summary>
        /// <param name="Filter"></param>
        /// <returns></returns>
        protected BaseMatcher ParseMatcher ( Func<Char, Boolean> Filter )
            => new FilterFuncMatcher ( Filter );

        #region Rule Registering

        /// <summary>
        /// Registers a rule from a string expression
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Expression"></param>
        /// <returns></returns>
        protected BaseMatcher Rule ( String Name, String Expression )
        {
            if ( String.IsNullOrWhiteSpace ( Name ) )
                throw new ArgumentException ( "Rule name can't be whitespaces, null or empty", nameof ( Name ) );
            if ( String.IsNullOrWhiteSpace ( Expression ) )
                throw new ArgumentException ( "Expression can't be whitespaces, null or empty", nameof ( Expression ) );

            return this.Rule ( Name, this.ParseMatcher ( Expression ) );
        }

        /// <summary>
        /// Registers a rule from a string expression along with
        /// it's factory
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Expression"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        protected BaseMatcher Rule ( String Name, String Expression, NodeFactory Factory )
        {
            this.Factory ( Name, Factory );
            return this.Rule ( Name, Expression );
        }

        /// <summary>
        /// Registers a rule from a matcher
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Matcher"></param>
        /// <returns></returns>
        protected BaseMatcher Rule ( String Name, BaseMatcher Matcher )
        {
            if ( String.IsNullOrWhiteSpace ( Name ) )
                throw new ArgumentException ( "Rule name can't be whitespaces, null or empty", nameof ( Name ) );
            if ( Matcher == null )
                throw new ArgumentNullException ( nameof ( Matcher ) );

            this.Rules[Name] = new RuleWrapper ( Matcher, Name );
            return new RulePlaceholder ( Name );
        }

        /// <summary>
        /// Registers a rule from a matcher along with it's factory
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Matcher"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        protected BaseMatcher Rule ( String Name, BaseMatcher Matcher, NodeFactory Factory )
        {
            this.Factory ( Name, Factory );
            return this.Rule ( Name, Matcher );
        }

        /// <summary>
        /// Sets the root rule from an expression
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Expression"></param>
        /// <returns></returns>
        protected BaseMatcher RootRule ( String Name, String Expression )
        {
            if ( String.IsNullOrWhiteSpace ( Name ) )
                throw new ArgumentException ( "Rule name can't be whitespaces, null or empty", nameof ( Name ) );
            if ( String.IsNullOrWhiteSpace ( Expression ) )
                throw new ArgumentException ( "Rule expression can't be whitespaces, null or empty", nameof ( Expression ) );

            this.RootName = Name;
            return this.Rule ( Name, Expression );
        }

        /// <summary>
        /// Sets the root rule from an expression along with it's factory
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Expression"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        protected BaseMatcher RootRule ( String Name, String Expression, NodeFactory Factory )
        {
            this.Factory ( Name, Factory );
            return this.RootRule ( Name, Expression );
        }

        /// <summary>
        /// Sets the root matcher
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Matcher"></param>
        /// <returns></returns>
        protected BaseMatcher RootRule ( String Name, BaseMatcher Matcher )
        {
            if ( String.IsNullOrWhiteSpace ( Name ) )
                throw new ArgumentException ( "Rule name can't be whitespaces, null or empty", nameof ( Name ) );
            if ( Matcher == null )
                throw new ArgumentNullException ( nameof ( Matcher ) );

            this.RootName = Name;
            return this.Rule ( Name, Matcher );
        }

        #endregion Rule Registering

        /// <summary>
        /// Returns the actual rule's matcher
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public BaseMatcher RawRule ( String Name )
        {
            if ( String.IsNullOrWhiteSpace ( Name ) )
                throw new ArgumentException ( "Rule name can't be whitespaces, null or empty", nameof ( Name ) );

            return this.Rules[Name];
        }

        #region Factory Management

        /// <summary>
        /// Retrieves a registered node factory
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        protected NodeFactory Factory ( String Name )
        {
            if ( String.IsNullOrWhiteSpace ( Name ) )
                throw new ArgumentException ( "Factory name can't be whitespaces, null or empty", nameof ( Name ) );

            return this.Factories[Name];
        }

        /// <summary>
        /// Registers a node factory
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Factory"></param>
        /// <returns></returns>
        protected NodeFactory Factory ( String Name, NodeFactory Factory )
        {
            if ( String.IsNullOrWhiteSpace ( Name ) )
                throw new ArgumentException ( "Factory name can't be whitespaces, null or empty", nameof ( Name ) );
            if ( Factory == null )
                throw new ArgumentNullException ( nameof ( Factory ) );

            return this.Factories[Name] = Factory;
        }

        #endregion Factory Management

        #region Debug

        private static readonly EBNFReconstructor EBNFReconstructor = new EBNFReconstructor ( );

        /// <summary>
        /// Returns the list of rules in this parser
        /// </summary>
        /// <returns></returns>
        public String[] GetRules ( )
        {
            var res = new List<String> ( this.Rules.Count );
            foreach ( KeyValuePair<String, BaseMatcher> rule in this.Rules )
                res.Add ( rule.Value.Accept ( EBNFReconstructor ) );
            return res.ToArray ( );
        }

        [ThreadStatic]
        private static readonly ExpressionReconstructor ExpressionReconstructor = new ExpressionReconstructor ( );

        /// <summary>
        /// Returns the expressions this parser is composed of
        /// </summary>
        /// <returns></returns>
        public String[] GetExpressions ( )
        {
            var res = new List<String> ( this.Rules.Count );
            foreach ( KeyValuePair<String, BaseMatcher> rule in this.Rules )
                res.Add ( $"{rule.Key} = {rule.Value.Accept ( ExpressionReconstructor )}" );
            return res.ToArray ( );
        }

        #endregion Debug
    }
}
