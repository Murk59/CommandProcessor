﻿namespace CommandProcessing.Tests.Filters
{
    using System;

    using CommandProcessing;
    using CommandProcessing.Filters;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HandlerExecutedContextFixture
    {
        [TestMethod]
        public void WhenCreatingInstanceThenPropertiesAreDefined()
        {
            // Arrange
            HandlerContext initialContext = new HandlerContext();
            HandlerExecutingContext preContext = new HandlerExecutingContext(initialContext);
            Exception exception = new Exception();

            // Act
            HandlerExecutedContext context = new HandlerExecutedContext(preContext, false, exception);

            // Assert
            Assert.IsNotNull(context.Result);
            Assert.IsInstanceOfType(context.Result, typeof(EmptyResult));
            Assert.IsNotNull(context.Exception);
            Assert.AreSame(context.Exception, exception);
            Assert.IsFalse(context.ExceptionHandled);
            Assert.IsFalse(context.Canceled);
        }

        [TestMethod]
        public void WhenSettingResultThenResultIsDefined()
        {
            // Arrange
            HandlerContext initialContext = new HandlerContext();
            HandlerExecutingContext preContext = new HandlerExecutingContext(initialContext);
            Exception exception = new Exception();
            HandlerExecutedContext context = new HandlerExecutedContext(preContext, false, exception);
            var value = "test";

            // Act
            context.Result = new HandlerResult("test");

            // Assert
            Assert.IsNotNull(context.Result);
            Assert.AreEqual(context.Result.Value, value);
        }
    }
}