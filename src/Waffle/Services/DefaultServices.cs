﻿namespace Waffle.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Waffle.Commands;
    using Waffle.Dependencies;
    using Waffle.Events;
    using Waffle.ExceptionHandling;
    using Waffle.Filters;
    using Waffle.Interception;
    using Waffle.Internal;
    using Waffle.Metadata;
    using Waffle.Properties;
    using Waffle.Queries;
    using Waffle.Queuing;
    using Waffle.Tracing;
    using Waffle.Validation;

    /// <summary>
    ///     <para>
    ///         Represents a container for service instances used by the <see cref="ProcessorConfiguration"/>. Note that
    ///         this container only supports known types, and methods to get or set arbitrary service types will
    ///         throw <see cref="ArgumentException"/> when called. For creation of arbitrary types, please use
    ///         <see cref="IDependencyResolver"/> instead. The supported types for this container are:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><see cref="IMessageProcessor"/></item>
    ///         <item><see cref="ICommandWorker"/></item>
    ///         <item><see cref="ICommandHandlerSelector"/></item>
    ///         <item><see cref="ICommandHandlerDescriptorProvider"/></item>
    ///         <item><see cref="ICommandHandlerActivator"/></item>
    ///         <item><see cref="ICommandHandlerTypeResolver"/></item>  
    ///         <item><see cref="IEventWorker"/></item>
    ///         <item><see cref="IEventHandlerSelector"/></item>
    ///         <item><see cref="IEventHandlerDescriptorProvider"/></item>
    ///         <item><see cref="IEventHandlerActivator"/></item>
    ///         <item><see cref="IEventHandlerTypeResolver"/></item>
    ///         <item><see cref="IFilterProvider"/></item>
    ///         <item><see cref="IAssembliesResolver"/></item>
    ///         <item><see cref="IProxyBuilder"/></item>
    ///         <item><see cref="IInterceptionProvider"/></item>
    ///         <item><see cref="IInterceptor"/></item>
    ///         <item><see cref="ICommandValidator"/></item>
    ///         <item><see cref="ModelValidatorProvider"/></item>
    ///         <item><see cref="IModelValidatorCache"/></item>
    ///         <item><see cref="IMessageProcessor"/></item>
    ///         <item><see cref="IPrincipalProvider"/></item>
    ///         <item><see cref="ModelMetadataProvider"/></item>
    ///         <item><see cref="IModelFlattener"/></item>
    ///         <item><see cref="IEventStore"/></item>
    ///         <item><see cref="IQueryService"/></item>
    ///     </list>
    ///     <para>
    ///         Passing any type which is not on this to any method on this interface will cause
    ///         an <see cref="ArgumentException"/> to be thrown.
    ///     </para>
    /// </summary>
    public sealed class DefaultServices : ServicesContainer
    {
        private ConcurrentDictionary<Type, object[]> cacheMulti = new ConcurrentDictionary<Type, object[]>();

        private ConcurrentDictionary<Type, object> cacheSingle = new ConcurrentDictionary<Type, object>();

        private readonly ProcessorConfiguration configuration;

        private readonly HashSet<Type> serviceTypesSingle;

        private readonly HashSet<Type> serviceTypesMulti;

        // Mutation operations delegate (throw if applied to wrong set)
        private readonly Dictionary<Type, object> defaultServicesSingle = new Dictionary<Type, object>();

        private readonly Dictionary<Type, List<object>> defaultServicesMulti = new Dictionary<Type, List<object>>();

        private IDependencyResolver lastKnownDependencyResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultServices"/> class.
        /// </summary>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="configuration"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Class needs references to large number of types.")]
        public DefaultServices(ProcessorConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            this.configuration = configuration;

            // Initialize the dictionary with all known service types, even if the list for that service type is
            // empty, because we will throw if the developer tries to read or write unsupported types.
            DefaultCommandHandlerSelector commandHandlerSelector = new DefaultCommandHandlerSelector(this.configuration);
            this.SetSingle<ICommandHandlerSelector>(commandHandlerSelector);
            this.SetSingle<ICommandHandlerDescriptorProvider>(commandHandlerSelector);
            this.SetSingle<ICommandHandlerActivator>(new DefaultCommandHandlerActivator());
            this.SetSingle<ICommandHandlerTypeResolver>(new DefaultCommandHandlerTypeResolver());

            // Events
            DefaultEventHandlerSelector eventHandlerSelector = new DefaultEventHandlerSelector(this.configuration);
            this.SetSingle<IEventHandlerSelector>(eventHandlerSelector);
            this.SetSingle<IEventHandlerDescriptorProvider>(eventHandlerSelector);
            this.SetSingle<IEventHandlerActivator>(new DefaultEventHandlerActivator());
            this.SetSingle<IEventHandlerTypeResolver>(new DefaultEventHandlerTypeResolver());
            this.SetSingle<ICommandHandlerInvoker>(new DefaultCommandHandlerInvoker());
            this.SetSingle<IEventHandlerInvoker>(new DefaultEventHandlerInvoker());

            this.SetMultiple<IFilterProvider>(new ConfigurationFilterProvider(), new HandlerFilterProvider());

            this.SetSingle<IAssembliesResolver>(new DefaultAssembliesResolver());

            this.SetSingle<IProxyBuilder>(new DefaultProxyBuilder());
            this.SetSingle<IInterceptionProvider>(new DefaultInterceptionProvider(this.configuration));
            this.SetMultiple<IInterceptor>(new IInterceptor[0]);
            this.SetSingle<ICommandValidator>(new DefaultCommandValidator());

            this.SetSingle<ModelMetadataProvider>(new DataAnnotationsModelMetadataProvider());
            this.SetSingle<IModelFlattener>(new DefaultModelFlattener());

            this.SetSingle<IPrincipalProvider>(new DefaultPrincipalProvider());

            this.SetSingle<IEventStore>(new NullEventStore());

            // Validation
            this.SetMultiple<ModelValidatorProvider>(new DataAnnotationsModelValidatorProvider());
            ModelValidatorCache validatorCache = new ModelValidatorCache(new Lazy<ModelValidatorProvider[]>(this.GetModelValidatorProviders));
            this.SetSingle<IModelValidatorCache>(validatorCache);

            // Tracing
            this.SetSingle<ITraceManager>(new TraceManager());
            this.SetSingle<ITraceWriter>(null);

            this.SetSingle<ICommandWorker>(new DefaultCommandWorker(configuration));
            this.SetSingle<IEventWorker>(new DefaultEventWorker(configuration));
            this.SetSingle<IMessageProcessor>(null);

            this.SetSingle<IQueryService>(new DefaultQueryService());

            // Exception handling
            this.SetSingle<IExceptionHandler>(null);
            this.SetMultiple<IExceptionLogger>();

            // Queuing
            this.SetSingle<ICommandReceiver>(null); 
            this.SetSingle<ICommandSender>(null);

            this.serviceTypesSingle = new HashSet<Type>(this.defaultServicesSingle.Keys);
            this.serviceTypesMulti = new HashSet<Type>(this.defaultServicesMulti.Keys);

            // Reset the caches and the known dependency scope
            this.ResetCache();
        }

        /// <summary>
        /// Queries whether a service type is single-instance.
        /// </summary>
        /// <param name="serviceType">
        /// Type of service to query.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the service type has at most one instance, or <see langword="false"/> if the service type supports multiple instances.
        /// </returns>
        public override bool IsSingleService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw Error.ArgumentNull("serviceType");
            }

            return this.serviceTypesSingle.Contains(serviceType);
        }

        /// <summary>
        /// Try to get a service of the given type.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <returns>The first instance of the service, or null if the service is not found.</returns>
        public override object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw Error.ArgumentNull("serviceType");
            }

            // Invalidate the cache if the dependency scope has switched
            if (this.lastKnownDependencyResolver != this.configuration.DependencyResolver)
            {
                this.ResetCache();
            }

            object result;

            if (this.cacheSingle.TryGetValue(serviceType, out result))
            {
                return result;
            }

            // Check input after initial read attempt for performance. 
            if (!this.serviceTypesSingle.Contains(serviceType))
            {
                throw Error.Argument("serviceType", Resources.DefaultServices_InvalidServiceType, serviceType.Name);
            }

            // Get the service from DI. If we're coming up hot, this might 
            // mean we end up creating the service more than once.
            object dependencyService = this.lastKnownDependencyResolver.GetService(serviceType);
            if (!this.cacheSingle.TryGetValue(serviceType, out result))
            {
                result = dependencyService ?? this.defaultServicesSingle[serviceType];
                this.cacheSingle.TryAdd(serviceType, result);
            }

            return result;
        }

        /// <summary>
        /// Try to get a list of services of the given type.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <returns>The list of service instances of the given type. Returns an empty enumeration if the
        /// service is not found. </returns>
        public override IEnumerable<object> GetServices(Type serviceType)
        {
            if (serviceType == null)
            {
                throw Error.ArgumentNull("serviceType");
            }

            // Invalidate the cache if the dependency scope has switched
            if (this.lastKnownDependencyResolver != this.configuration.DependencyResolver)
            {
                this.ResetCache();
            }

            object[] result;
            if (this.cacheMulti.TryGetValue(serviceType, out result))
            {
                return result;
            }

            if (!this.serviceTypesMulti.Contains(serviceType))
            {
                throw Error.Argument("serviceType", Resources.DefaultServices_InvalidServiceType, serviceType.Name);
            }

            // Get the service from DI If we're coming up hot, this might
            // mean we end up creating the service more than once.
            IEnumerable<object> dependencyServices = this.lastKnownDependencyResolver.GetServices(serviceType);

            if (!this.cacheMulti.TryGetValue(serviceType, out result))
            {
                result = dependencyServices.Where(s => s != null).Concat(this.defaultServicesMulti[serviceType]).ToArray();
                this.cacheMulti.TryAdd(serviceType, result);
            }

            return result;
        }

        /// <summary>
        /// Returns the list of object for the given service type. Also validates <paramref name="serviceType"/> is in the known service type list.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <returns>The list of service instances of the given type. </returns>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "inherits from base")]
        protected override List<object> GetServiceInstances(Type serviceType)
        {
            if (serviceType == null)
            {
                throw Error.ArgumentNull("serviceType");
            }

            List<object> result;
            if (!this.defaultServicesMulti.TryGetValue(serviceType, out result))
            {
                throw Error.Argument("serviceType", Resources.DefaultServices_InvalidServiceType, serviceType.Name);
            }

            return result;
        }

        /// <summary>
        /// Removes a single-instance service from the default services.
        /// </summary>
        /// <param name="serviceType">The type of service.</param>
        protected override void ClearSingle(Type serviceType)
        {
            this.defaultServicesSingle[serviceType] = null;
        }

        /// <summary>
        /// Replaces a single-instance service object.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <param name="service">The service object that replaces the previous instance.</param>
        protected override void ReplaceSingle(Type serviceType, object service)
        {
            if (serviceType == null)
            {
                throw Error.ArgumentNull("serviceType");
            }

            this.defaultServicesSingle[serviceType] = service;
        }

        /// <summary>
        /// Removes the cached values for a single service type. Called whenever the user manipulates 
        /// the local service list for a given service type.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        protected override void ResetCache(Type serviceType)
        {
            object single;
            this.cacheSingle.TryRemove(serviceType, out single);
            object[] multiple;
            this.cacheMulti.TryRemove(serviceType, out multiple);
        }

        private void SetSingle<T>(T instance) where T : class
        {
            this.defaultServicesSingle[typeof(T)] = instance;
        }

        private void SetMultiple<T>(params T[] instances) where T : class
        {
            IEnumerable<object> x = instances;
            this.defaultServicesMulti[typeof(T)] = new List<object>(x);
        }

        /// <summary>
        /// Removes the cached values for all service types. Called when the dependency scope
        /// has changed since the last time we made a request.
        /// </summary>
        private void ResetCache()
        {
            this.cacheSingle = new ConcurrentDictionary<Type, object>();
            this.cacheMulti = new ConcurrentDictionary<Type, object[]>();
            this.lastKnownDependencyResolver = this.configuration.DependencyResolver;
        }
    }
}