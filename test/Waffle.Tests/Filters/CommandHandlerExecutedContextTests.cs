﻿namespace Waffle.Tests.Filters
{
    using System;
    using System.Runtime.ExceptionServices;
    using Waffle.Filters;
    using Xunit;

    public class CommandHandlerExecutedContextTests
    {
        [Fact]
        public void WhenCreatingInstanceThenPropertiesAreDefined()
        {
            // Arrange
            CommandHandlerContext preContext = new CommandHandlerContext();
            Exception exception = new Exception();
            ExceptionDispatchInfo exceptionInfo = ExceptionDispatchInfo.Capture(exception);

            // Act
            CommandHandlerExecutedContext context = new CommandHandlerExecutedContext(preContext, exceptionInfo);

            // Assert
            Assert.Null(context.Response);
            Assert.NotNull(context.ExceptionInfo.SourceException);
            Assert.Same(context.ExceptionInfo.SourceException, exception);
        }

#if LOOSE_CQRS
        [Fact]
        public void WhenSettingResultThenResultIsDefined()
        {
            // Arrange
            CommandHandlerContext preContext = new CommandHandlerContext();
            Exception exception = new Exception();
            ExceptionDispatchInfo exceptionInfo = ExceptionDispatchInfo.Capture(exception);
            CommandHandlerExecutedContext context = new CommandHandlerExecutedContext(preContext, exceptionInfo);
            var value = "test";

            // Act
            context.Response = context.Request.CreateResponse("test");

            // Assert
            Assert.Equal(context.Response.Value, value);
        }
#endif
    }
}