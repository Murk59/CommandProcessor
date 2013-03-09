﻿namespace CommandProcessing
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using CommandProcessing.Services;

    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "ServicesContainer is disposed with the configuration")]
    public sealed class ServiceSettings
    {
        private readonly ProcessorConfiguration configuration;

        private ServicesContainer services;

        public ServiceSettings(ProcessorConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this.configuration = configuration;
        }

        public bool AbortOnInvalidCommand { get; set; }

        public bool ServiceProxyCreationEnabled { get; set; }

        public ServicesContainer Services
        {
            get
            {
                if (this.services == null)
                {
                    this.services = new HandlerServices(this.configuration.Services);
                }

                return this.services;
            }
        }

        internal bool IsServiceCollectionInitialized
        {
            get
            {
                return this.services != null;
            }
        }
    }
}