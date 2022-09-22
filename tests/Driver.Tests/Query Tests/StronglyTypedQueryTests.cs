using System.Globalization;

using SurrealDB.Common;

namespace SurrealDB.Driver.Tests.Queries;

public class RestStringQueryTests : StringQueryTests<DatabaseRest, RestResponse> { }
public class RpcStringQueryTests : StringQueryTests<DatabaseRpc, RpcResponse> { }

public class RpcGuidQueryTests : GuidQueryTests<DatabaseRpc, RpcResponse> { }
public class RestGuidQueryTests : GuidQueryTests<DatabaseRest, RestResponse> { }

public class RpcDateTimeQueryTests : DateTimeQueryTests<DatabaseRpc, RpcResponse> { }
public class RestDateTimeQueryTests : DateTimeQueryTests<DatabaseRest, RestResponse> { }

public class RpcIntQueryTests : IntQueryTests<DatabaseRpc, RpcResponse> { }
public class RestIntQueryTests : IntQueryTests<DatabaseRest, RestResponse> { }

public class RpcLongQueryTests : LongQueryTests<DatabaseRpc, RpcResponse> { }
public class RestLongQueryTests : LongQueryTests<DatabaseRest, RestResponse> { }

public class RpcFloatQueryTests : FloatQueryTests<DatabaseRpc, RpcResponse> { }
public class RestFloatQueryTests : FloatQueryTests<DatabaseRest, RestResponse> { }

public class RpcDoubleQueryTests : DoubleQueryTests<DatabaseRpc, RpcResponse> { }
public class RestDoubleQueryTests : DoubleQueryTests<DatabaseRest, RestResponse> { }

public abstract class StringQueryTests <T, U> : EqualityQueryTests<T, U, string, string>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override string RandomKey() {
        return RandomString();
    }

    protected override string RandomValue() {
        return RandomString();
    }

    private static string RandomString(int length = 10) {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[RngHelper.Shared.Next(s.Length)]).ToArray());
    }
}

public abstract class IntQueryTests <T, U> : MathQueryTests<T, U, int, int>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override int RandomValue() {
        return RngHelper.Shared.Next(-10000, 10000); // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static int RandomInt() {
        return RngHelper.Shared.Next();
    }

    protected override void AssertEquivalency(int a, int b) {
        b.Should().Be(a);
    }
}

public abstract class LongQueryTests <T, U> : MathQueryTests<T, U, long, long>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override long RandomKey() {
        return RandomLong();
    }

    protected override long RandomValue() {
        return RngHelper.Shared.NextInt64(-10000, 10000); // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static long RandomLong() {
        return RngHelper.Shared.NextInt64();
    }

    protected override void AssertEquivalency(long a, long b) {
        b.Should().Be(a);
    }
}

public abstract class FloatQueryTests <T, U> : MathQueryTests<T, U, float, float>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override float RandomKey() {
        return RandomFloat();
    }

    protected override float RandomValue() {
        return (RandomFloat() * 2000) - 1000; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static float RandomFloat() {
        return RngHelper.Shared.NextSingle();
    }

    protected override void AssertEquivalency(float a, float b) {
        b.Should().BeApproximately(a, 0.1f);
    }
}

public abstract class DoubleQueryTests <T, U> : MathQueryTests<T, U, double, double>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override double RandomKey() {
        return RandomDouble();
    }

    protected override double RandomValue() {
        return (RandomDouble() * 2000d) - 1000d; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static double RandomDouble() {
        return RngHelper.Shared.NextDouble();
    }

    protected override void AssertEquivalency(double a, double b) {
        b.Should().BeApproximately(a, 0.1f);
    }
}

public abstract class GuidQueryTests<T, U> : EqualityQueryTests<T, U, Guid, Guid>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override Guid RandomKey() {
        return RandomGuid();
    }

    protected override Guid RandomValue() {
        return RandomGuid();
    }

    private static Guid RandomGuid() {
        return Guid.NewGuid();
    }
}

public abstract class DateTimeQueryTests<T, U> : InequalityQueryTests<T, U, int, DateTime>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override DateTime RandomValue() {
        return RandomDateTime();
    }

    private static int RandomInt() {
        return RngHelper.Shared.Next();
    }

    private static DateTime RandomDateTime() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(RngHelper.Shared.NextDouble() * diff));
        return randomeDateTime;
    }
}