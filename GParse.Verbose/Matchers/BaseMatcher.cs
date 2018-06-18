﻿using System;
using GParse.Common.IO;
using GParse.Verbose.Abstractions;

namespace GParse.Verbose.Matchers
{
    public abstract class BaseMatcher : IPatternMatcher
    {
        #region Pattern Matchers Composition

        #region Pattern Repetition

        /// <summary>
        /// Makes this pattern optional
        /// </summary>
        /// <returns></returns>
        public BaseMatcher Optional ( ) => new OptionalMatcher ( this );

        /// <summary>
        /// Alias for <see cref="Optional" />
        /// </summary>
        /// <param name="matcher"></param>
        /// <returns></returns>
        public static BaseMatcher operator ~ ( BaseMatcher matcher ) => matcher.Optional ( );

        /// <summary>
        /// Makes so that this pattern is executed as many times
        /// as posible.
        /// </summary>
        /// <returns></returns>
        public BaseMatcher Infinite ( ) => new RepeatedMatcher ( this );

        /// <summary>
        /// Enables this pattern to be matched at most
        /// <paramref name="maximum" /> times
        /// </summary>
        /// <param name="maximum"></param>
        /// <returns></returns>
        public BaseMatcher Repeat ( Int32 maximum ) => new RepeatedMatcher ( this, -1, maximum );

        /// <summary>
        /// Indicates this pattern should be matched at least
        /// <paramref name="minimum" /> times and at most
        /// <paramref name="maximum" /> times
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        public BaseMatcher Repeat ( Int32 minimum, Int32 maximum ) => new RepeatedMatcher ( this, minimum, maximum );

        /// <summary>
        /// Enables this pattern to be matched infinite or a
        /// limited number of times (-1 for infinite) (optional by
        /// default, use <see cref="Repeat(Int32, Int32)" /> to
        /// indicate a minimum match count)
        /// </summary>
        /// <param name="matcher"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static BaseMatcher operator * ( BaseMatcher matcher, Int32 limit )
            => limit == -1 ? matcher.Infinite ( ) : matcher.Repeat ( limit );

        /// <summary>
        /// Enables this pattern to be matched from
        /// <paramref name="range" />.min to
        /// <paramref name="range" />.max times
        /// </summary>
        /// <param name="matcher"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static BaseMatcher operator * ( BaseMatcher matcher, (Int32 min, Int32 max) range )
            => matcher.Repeat ( range.min, range.max );

        #endregion Pattern Repetition

        #region Pattern Negation

        /// <summary>
        /// Negates the current pattern
        /// </summary>
        /// <returns></returns>
        public BaseMatcher Negate ( ) => new NegatedMatcher ( this );

        /// <summary>
        /// Alias for <see cref="Negate" />
        /// </summary>
        /// <param name="operand"></param>
        /// <returns></returns>
        public static BaseMatcher operator - ( BaseMatcher operand ) => operand.Negate ( );

        public static BaseMatcher operator ! ( BaseMatcher operand ) => operand.Negate ( );

        #endregion Pattern Negation

        #region Sequentiation

        /// <summary>
        /// Makes sure this pattern will be followed by <paramref name="matcher" />
        /// </summary>
        /// <param name="matcher"></param>
        /// <returns></returns>
        public BaseMatcher Then ( BaseMatcher matcher )
        {
            BaseMatcher[] arr;
            if ( this is AllMatcher all )
            {
                // Create a new array with an expanded size
                arr = new BaseMatcher[all.PatternMatchers.Length + 1];
                // Copy over the old array
                Array.Copy ( all.PatternMatchers, arr, all.PatternMatchers.Length );
                // Then insert the element to be added at the end
                arr[all.PatternMatchers.Length] = matcher;
            }
            else
                arr = new[] { this, matcher };
            return new AllMatcher ( arr );
        }

        /// <summary>
        /// Alias for <see cref="Then(BaseMatcher)" />
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static BaseMatcher operator + ( BaseMatcher lhs, BaseMatcher rhs ) => lhs.Then ( rhs );

        /// <summary>
        /// Alias for <see cref="Then(BaseMatcher)" />
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static BaseMatcher operator & ( BaseMatcher lhs, BaseMatcher rhs ) => lhs.Then ( rhs );

        /// <summary>
        /// Makes sure that the what's next won't match <paramref name="matcher" />
        /// </summary>
        /// <param name="matcher"></param>
        /// <returns></returns>
        public BaseMatcher NotThen ( BaseMatcher matcher ) => this.Then ( !matcher );

        /// <summary>
        /// Alias for <see cref="NotThen(BaseMatcher)" />
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static BaseMatcher operator - ( BaseMatcher lhs, BaseMatcher rhs ) => lhs.NotThen ( rhs );

        /// <summary>
        /// Matches either the current pattern or the <paramref name="alternative" />
        /// </summary>
        /// <param name="alternative"></param>
        /// <returns></returns>
        public BaseMatcher Or ( BaseMatcher alternative )
        {
            BaseMatcher[] arr;
            if ( this is AnyMatcher any )
            {
                arr = new BaseMatcher[any.PatternMatchers.Length + 1];
                Array.Copy ( any.PatternMatchers, arr, any.PatternMatchers.Length );
                arr[any.PatternMatchers.Length] = alternative;
            }
            else
                arr = new[] { this, alternative };
            return new AnyMatcher ( arr );
        }

        /// <summary>
        /// Alias of <see cref="Or(BaseMatcher)" />
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static BaseMatcher operator | ( BaseMatcher lhs, BaseMatcher rhs ) => lhs.Or ( rhs );

        #endregion Sequentiation

        #region Content Modification

        public BaseMatcher Ignore ( ) => new IgnoreMatcher ( this );

        public BaseMatcher Join ( ) => new JoinMatcher ( this );

        #endregion Content Modification

        #region Marker Node

        /// <summary>
        /// Emits a <see cref="AST.MarkerNode" /> with the first
        /// element of the match array when matched.
        /// </summary>
        /// <param name="parser"></param>
        /// <returns></returns>
        public BaseMatcher Mark ( VerboseParser parser ) => new MarkerMatcher ( parser, this );

        #endregion Marker Node

        #endregion Pattern Matchers Composition

        #region IPatternMatcher API

        public abstract Int32 MatchLength ( SourceCodeReader reader, Int32 offset = 0 );

        public abstract String[] Match ( SourceCodeReader reader );

        #endregion IPatternMatcher API

        public abstract override Boolean Equals ( Object obj );

        public abstract override Int32 GetHashCode ( );
    }
}
