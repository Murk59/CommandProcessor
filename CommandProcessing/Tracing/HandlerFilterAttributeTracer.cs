﻿namespace CommandProcessing.Tracing
{
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using CommandProcessing.Filters;
    using CommandProcessing.Internal;

    /// <summary>
    /// Tracer for <see cref="Filters.HandlerFilterAttribute"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "internal type needs to override, tracer are not sealed")]
    internal class HandlerFilterAttributeTracer : HandlerFilterAttribute, IDecorator<HandlerFilterAttribute>
    {
        private const string ActionExecutedMethodName = "ActionExecuted";

        private const string ActionExecutingMethodName = "ActionExecuting";

        private readonly HandlerFilterAttribute innerFilter;

        private readonly ITraceWriter traceWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerFilterAttributeTracer"/> class.
        /// </summary>
        /// <param name="innerFilter">
        /// The inner filter.
        /// </param>
        /// <param name="traceWriter">
        /// The trace writer.
        /// </param>
        public HandlerFilterAttributeTracer(HandlerFilterAttribute innerFilter, ITraceWriter traceWriter)
        {
            Contract.Assert(innerFilter != null);
            Contract.Assert(traceWriter != null);

            this.innerFilter = innerFilter;
            this.traceWriter = traceWriter;
        }

        /// <summary>
        /// Gets a value indicating whether allow multiple.
        /// </summary>
        /// <value>
        /// The allow multiple.
        /// </value>
        public override bool AllowMultiple
        {
            get
            {
                return this.innerFilter.AllowMultiple;
            }
        }

        /// <summary>
        /// Gets the inner.
        /// </summary>
        /// <value>
        /// The inner.
        /// </value>
        public HandlerFilterAttribute Inner
        {
            get
            {
                return this.innerFilter;
            }
        }

        /// <summary>
        /// When implemented in a derived class, gets a unique identifier for this <see cref="T:System.Attribute"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Object"/> that is a unique identifier for the attribute.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override object TypeId
        {
            get
            {
                return this.innerFilter.TypeId;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> equals the type and value of this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="obj">An <see cref="T:System.Object"/> to compare with this instance or null. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            return this.innerFilter.Equals(obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return this.innerFilter.GetHashCode();
        }

        /// <summary>
        /// When overridden in a derived class, indicates whether the value of this instance is the default value for the derived class.
        /// </summary>
        /// <returns>
        /// <c>true</c>  if this instance is the default attribute for the class; otherwise, <c>false</c>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override bool IsDefaultAttribute()
        {
            return this.innerFilter.IsDefaultAttribute();
        }

        /// <summary>
        /// When overridden in a derived class, returns a value that indicates whether this instance equals a specified object.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instance equals <paramref name="obj"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="obj">An <see cref="T:System.Object"/> to compare with this instance of <see cref="T:System.Attribute"/>. </param><filterpriority>2</filterpriority>
        public override bool Match(object obj)
        {
            return this.innerFilter.Match(obj);
        }

        /// <summary>
        /// The on command executed.
        /// </summary>
        /// <param name="actionExecutedContext">
        /// The handler executed context.
        /// </param>
        public override void OnCommandExecuted(HandlerExecutedContext actionExecutedContext)
        {
            this.traceWriter.TraceBeginEnd(
                actionExecutedContext.Request, 
                TraceCategories.FiltersCategory, 
                TraceLevel.Info, 
                this.innerFilter.GetType().Name, 
                ActionExecutedMethodName, 
                beginTrace: (tr) =>
                {
                    tr.Message = Internal.Error.Format(Resources.TraceActionFilterMessage, FormattingUtilities.HandlerDescriptorToString(actionExecutedContext.HandlerContext.Descriptor));
                    tr.Exception = actionExecutedContext.Exception;
                    object response = actionExecutedContext.Result;
                }, 
                execute: () => this.innerFilter.OnCommandExecuted(actionExecutedContext), 
                endTrace: null, 
                errorTrace: null);
        }

        /// <summary>
        /// The on command executing.
        /// </summary>
        /// <param name="handlerContext">
        /// The handler context.
        /// </param>
        public override void OnCommandExecuting(HandlerContext handlerContext)
        {
            if (handlerContext == null)
            {
                throw Error.ArgumentNull("handlerContext");
            }

            this.traceWriter.TraceBeginEnd(
                handlerContext.Request, 
                TraceCategories.FiltersCategory, 
                TraceLevel.Info, 
                this.innerFilter.GetType().Name, 
                ActionExecutingMethodName, 
                beginTrace: (tr) => 
                {
                    tr.Message = Internal.Error.Format(Resources.TraceActionFilterMessage, FormattingUtilities.HandlerDescriptorToString(handlerContext.Descriptor)); 
                },
                execute: () => this.innerFilter.OnCommandExecuting(handlerContext), 
                endTrace: null, 
                errorTrace: null);
        }
    }
}