﻿namespace CommandProcessing.Tests.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using CommandProcessing;
    using CommandProcessing.Dispatcher;
    using CommandProcessing.Filters;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DefaultHandlerSelectorFixture
    {
        private readonly ProcessorConfiguration config = new ProcessorConfiguration();

        [TestMethod]
        public void WhenSelectingUnknownHandlerThenThrowsInvalidOperationException()
        {
            // Assign
            DefaultHandlerSelector resolver = this.CreateTestableService();
            HandlerRequest request = new HandlerRequest(this.config, new SimpleCommand());
            this.config.Services.Replace(typeof(IHandlerTypeResolver), new EmptyHandlerTypeResolver());
            bool exceptionRaised = false;

            // Act
            try
            {
                resolver.SelectHandler(request);
            }
            catch (InvalidOperationException)
            {
                exceptionRaised = true;
            }

            // Assert
            Assert.IsTrue(exceptionRaised);
        }

        [TestMethod]
        public void WhenSelectingDuplicateHandlerThenThrowsInvalidOperationException()
        {
            // Assign
            DefaultHandlerSelector resolver = this.CreateTestableService();
            HandlerRequest request = new HandlerRequest(this.config, new SimpleCommand());
            this.config.Services.Replace(typeof(IHandlerTypeResolver), new DuplicateHandlerTypeResolver());
            bool exceptionRaised = false;

            // Act
            try
            {
                resolver.SelectHandler(request);
            }
            catch (InvalidOperationException)
            {
                exceptionRaised = true;
            }

            // Assert
            Assert.IsTrue(exceptionRaised);
        }

        [TestMethod]
        public void WhenSelectingHandlerThenReturnHandlerDesciptor()
        {
            // Assign
            DefaultHandlerSelector resolver = this.CreateTestableService();
            HandlerRequest request = new HandlerRequest(this.config, new SimpleCommand());
            this.config.Services.Replace(typeof(IHandlerTypeResolver), new SimpleHandlerTypeResolver());

            // Act
            var descriptor = resolver.SelectHandler(request);


            // Assert
            Assert.IsNotNull(descriptor);
            Assert.AreEqual(typeof(SimpleHandler1), descriptor.HandlerType);
        }

        [TestMethod]
        public void WhenSelectingHandlerWiuthNullParamterThenThrowsArgumentNullException()
        {
            // Assign
            DefaultHandlerSelector resolver = this.CreateTestableService();
            HandlerRequest request = null;
            bool exceptionRaised = false;

            // Act
            try
            {
                resolver.SelectHandler(request);
            }
            catch (ArgumentNullException)
            {
                exceptionRaised = true;
            }

            // Assert
            Assert.IsTrue(exceptionRaised);
        }
        
        [TestMethod]
        public void WhenGettingHandlerMappingThenReturnsMapping()
        {
            // Assign
            DefaultHandlerSelector resolver = this.CreateTestableService();
            this.config.Services.Replace(typeof(IHandlerTypeResolver), new MultipleHandlerTypeResolver());

            // Act
            var mapping = resolver.GetHandlerMapping();
            
            // Assert
            Assert.IsNotNull(mapping);
            Assert.AreEqual(2, mapping.Count);
            Assert.IsTrue(mapping.ContainsKey(typeof(SimpleCommand)));
            Assert.IsTrue(mapping.ContainsKey(typeof(SimpleCommand2)));

            Assert.AreEqual(mapping[typeof(SimpleCommand)].HandlerType, typeof(SimpleHandler1));
            Assert.AreEqual(mapping[typeof(SimpleCommand2)].HandlerType, typeof(SimpleHandler3));
        }

        private DefaultHandlerSelector CreateTestableService()
        {
            return new DefaultHandlerSelector(this.config);
        }

        private class SimpleCommand : Command
        {
        }

        private class SimpleCommand2 : Command
        {
        }

        private class SimpleHandler1 : Handler<SimpleCommand>
        {
            public override void Handle(SimpleCommand command)
            {
                throw new NotImplementedException();
            }
        }

        private class SimpleHandler2 : Handler<SimpleCommand>
        {
            public override void Handle(SimpleCommand command)
            {
                throw new NotImplementedException();
            }
        }

        private class SimpleHandler3 : Handler<SimpleCommand2>
        {
            public override void Handle(SimpleCommand2 command)
            {
                throw new NotImplementedException();
            }
        }

        private class EmptyHandlerTypeResolver : IHandlerTypeResolver
        {
            public ICollection<Type> GetHandlerTypes(IAssembliesResolver assembliesResolver)
            {
                return Type.EmptyTypes;
            }
        }

        private class DuplicateHandlerTypeResolver : IHandlerTypeResolver
        {
            public ICollection<Type> GetHandlerTypes(IAssembliesResolver assembliesResolver)
            {
                return new[] { typeof(SimpleHandler1), typeof(SimpleHandler2) };
            }
        }

        private class SimpleHandlerTypeResolver : IHandlerTypeResolver
        {
            public ICollection<Type> GetHandlerTypes(IAssembliesResolver assembliesResolver)
            {
                return new[] { typeof(SimpleHandler1) };
            }
        }

        private class MultipleHandlerTypeResolver : IHandlerTypeResolver
        {
            public ICollection<Type> GetHandlerTypes(IAssembliesResolver assembliesResolver)
            {
                return new[] { typeof(SimpleHandler1), typeof(SimpleHandler3) };
            }
        }
    }
}