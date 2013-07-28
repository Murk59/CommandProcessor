﻿namespace Waffle.Unity.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Waffle;
    using Waffle.Commands;
    using Waffle.Filters;

    [TestClass]
    public sealed class CommandProcessorWithUnityFixture : IDisposable
    {
        private readonly ICollection<IDisposable> disposableResources = new Collection<IDisposable>();

        private readonly ProcessorConfiguration configuration = new ProcessorConfiguration();
        
        private readonly IUnityContainer container = new UnityContainer();

        private readonly Mock<ICommandHandlerTypeResolver> resolver = new Mock<ICommandHandlerTypeResolver>();

        [TestMethod]
        public void WhenProcessingValidCommandThenCommandIsProcessed()
        {
            // Arrange
            this.resolver
                .Setup(r => r.GetCommandHandlerTypes(It.IsAny<IAssembliesResolver>()))
                .Returns(new[] { typeof(ValidCommandHandler) });
            this.configuration.Services.Replace(typeof(ICommandHandlerTypeResolver), this.resolver.Object);
            var service = new Mock<ISimpleService>();
            
            this.container.RegisterInstance(service.Object);
            MessageProcessor processor = this.CreateTestableProcessor();
            ValidCommand command = new ValidCommand();

            // Act
            var result = processor.Process<string>(command);

            // Assert
            Assert.AreEqual("OK", result);
            service.Verify(s => s.Execute(), Times.Once());
        }
        
        [TestMethod]
        public void WhenProcessingCommandWithoutResultThenCommandIsProcessed()
        {
            // Arrange
            this.resolver
                .Setup(r => r.GetCommandHandlerTypes(It.IsAny<IAssembliesResolver>()))
                .Returns(new[] { typeof(ValidCommandHandlerWithoutResult) });
            this.configuration.Services.Replace(typeof(ICommandHandlerTypeResolver), this.resolver.Object);
            var service = new Mock<ISimpleService>();

            this.container.RegisterInstance(service.Object);
            MessageProcessor processor = this.CreateTestableProcessor();
            ValidCommand command = new ValidCommand();

            // Act
            processor.Process(command);

            // Assert
            service.Verify(s => s.Execute(), Times.Once());
        }
        
        private MessageProcessor CreateTestableProcessor(ProcessorConfiguration config = null)
        {
            try
            {
                config = config ?? this.configuration;
                config.RegisterContainer(this.container);
                MessageProcessor processor = new MessageProcessor(config);
                this.disposableResources.Add(processor);
                config = null;
                return processor;
            }
            finally
            {
                if (config != null)
                {
                    config.Dispose();
                }
            }
        }

        public class InvalidCommand : Command
        {
            [Required]
            public string Property { get; set; }
        }

        public class ValidCommand : Command
        {
            public ValidCommand()
            {
                this.Property = "test";
            }

            [Required]
            public string Property { get; set; }
        }

        public class ValidCommandHandler : CommandHandler<ValidCommand, string>
        {
            public ValidCommandHandler(ISimpleService service)
            {
                this.Service = service;
            }

            public ISimpleService Service { get; set; }

            public override string Handle(ValidCommand command, CommandHandlerContext context)
            {
                this.Service.Execute();
                return "OK";
            }
        }

        public class ValidCommandHandlerWithoutResult : CommandHandler<ValidCommand>
        {
            public ValidCommandHandlerWithoutResult(ISimpleService service)
            {
                this.Service = service;
            }

            public ISimpleService Service { get; set; }

            public override void Handle(ValidCommand command, CommandHandlerContext context)
            {
                this.Service.Execute();
            }
        }

        public interface ISimpleService
        {
            void Execute();
        }

        public class SimpleService : ISimpleService
        {
            public void Execute()
            {
            }
        }

        [TestCleanup]
        public void Dispose()
        {
            this.configuration.Dispose();
            foreach (IDisposable disposable in this.disposableResources)
            {
                disposable.Dispose();
            }
        }
    }
}