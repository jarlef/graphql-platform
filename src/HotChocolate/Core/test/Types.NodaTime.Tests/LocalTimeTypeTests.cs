using HotChocolate.Execution;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalTimeTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public LocalTime One => LocalTime
                    .FromHourMinuteSecondMillisecondTick(12, 42, 13, 31, 100)
                    .PlusNanoseconds(1234);
            }

            public class Mutation
            {
                public LocalTime Test(LocalTime arg)
                {
                    return arg + Period.FromMinutes(10);
                }
            }
        }

        private readonly IRequestExecutor _testExecutor =
            SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();

        [Fact]
        public void QueryReturns()
        {
            var result = _testExecutor.Execute("query { test: one }");

            Assert.Equal("12:42:13.031011234", result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: LocalTime!) { test(arg: $arg) }")
                        .SetVariableValues(new Dictionary<string, object?> { { "arg", "12:42:13.031011234" }, })
                        .Build());

            Assert.Equal("12:52:13.031011234", result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithoutTicks()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: LocalTime!) { test(arg: $arg) }")
                        .SetVariableValues(new Dictionary<string, object?> { { "arg", "12:42:13" }, })
                        .Build());

            Assert.Equal("12:52:13", result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: LocalTime!) { test(arg: $arg) }")
                        .SetVariableValues(new Dictionary<string, object?> { { "arg", "12:42" }, })
                        .Build());

            Assert.Null(result.ExpectOperationResult().Data);
            Assert.Single(result.ExpectOperationResult().Errors!);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation { test(arg: \"12:42:13.031011234\") }")
                        .Build());

            Assert.Equal("12:52:13.031011234", result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithoutTick()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation { test(arg: \"12:42:13\") }")
                        .Build());

            Assert.Equal("12:52:13", result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation { test(arg: \"12:42\") }")
                        .Build());

            Assert.Null(result.ExpectOperationResult().Data);
            Assert.Single(result.ExpectOperationResult().Errors!);
            Assert.Null(result.ExpectOperationResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalTime",
                result.ExpectOperationResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmptyThrowSchemaException()
        {
            static object Call() => new LocalTimeType([]);
            Assert.Throws<SchemaException>(Call);
        }
    }
}
