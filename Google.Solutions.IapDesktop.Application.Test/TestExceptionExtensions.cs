using NUnit.Framework;
using System;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Test
{
    [TestFixture]
    public class TestExceptionExtensions
    {
        [Test]
        public void WhenRegularException_UnwrapDoesNothing()
        {
            var ex = new ApplicationException();

            var unwrapped = ex.Unwrap();

            Assert.AreSame(ex, unwrapped);
        }

        [Test]
        public void WhenAggregateException_UnwrapReturnsFirstInnerException()
        {
            var inner1 = new ApplicationException();
            var inner2 = new ApplicationException();
            var aggregate = new AggregateException(inner1, inner2);

            var unwrapped = aggregate.Unwrap();

            Assert.AreSame(inner1, unwrapped);
        }

        [Test]
        public void WhenTargetInvocationException_UnwrapReturnsInnerException()
        {
            var inner = new ApplicationException();
            var target = new TargetInvocationException("", inner);

            var unwrapped = target.Unwrap();

            Assert.AreSame(inner, unwrapped);
        }
    }
}
