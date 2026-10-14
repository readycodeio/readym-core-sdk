using System.Reflection;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.Interning;
using ReadyM.Api.Serialization;
using Xunit;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;
using Yooni.Native.Serialization;
using static ReadyM.Api.Generators.Tests.DeriveTestAssert;

namespace ReadyM.Api.Generators.Tests;

// A field marked [Interned] goes out as an id when the entered index has the string, and as the string otherwise
public sealed class DeriveINetworkedComponentInternedTests(ITestOutputHelper output)
{
    private const string Source = """
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct InternedComponent(AllocatorKind kind) : INetworkedComponent
{
    private int _revision;
    [Interned] private NativeString256 _gait;
    [Interned] private NativeList<NativeString256> _tags = new(2, kind);
    private NativeString256 _plain;
}
""";

    private static readonly NativeString256 Walk = new("Actor.Locomotion.Gait.Walk", true);
    private static readonly NativeString256 Run = new("Actor.Locomotion.Gait.Run", true);
    private static readonly NativeString256 Moving = new("Actor.Locomotion.Moving", true);
    private static readonly NativeString256 Plain = new("Plain", false);

    [Fact]
    public void With_no_index_every_marked_string_is_written_inline_and_unmarked_ones_as_before()
    {
        var component = Create(7, Walk, [Moving, Walk], Plain);

        var expected = new NetDataWriter();
        expected.Put(7);
        PutInline(expected, Walk);
        expected.Put(2);
        PutInline(expected, Moving);
        PutInline(expected, Walk);
        Plain.Serialize(expected);

        Assert.Equal(expected.CopyData(), InvokeSerialize(component));
    }

    [Fact]
    public void The_server_index_writes_ids_and_queues_each_new_string_once()
    {
        var component = Create(7, Walk, [Moving, Walk], Plain);
        var server = new ServerInternedStringIndex(100, _log);

        byte[] bytes;
        using (InternedStrings.Enter(server))
            bytes = InvokeSerialize(component);

        var expected = new NetDataWriter();
        expected.Put(7);
        PutId(expected, 0);
        expected.Put(2);
        PutId(expected, 1);
        PutId(expected, 0);
        Plain.Serialize(expected);

        Assert.Equal(expected.CopyData(), bytes);
        Assert.Equal(2, server.Count);
        Assert.True(server.HasPendingAppend);
        Assert.True(server.TryResolve(0, out var first) && first == Walk);
        Assert.True(server.TryResolve(1, out var second) && second == Moving);
    }

    [Fact]
    public void A_client_writes_ids_only_for_strings_the_server_sent_it_and_adds_nothing()
    {
        var server = new ServerInternedStringIndex(100, _log);
        server.TryGetIdForWrite(Walk, out _);
        var client = ClientCopyOf(server);

        var component = Create(7, Run, [Moving, Walk], Plain);

        byte[] bytes;
        using (InternedStrings.Enter(client))
            bytes = InvokeSerialize(component);

        var expected = new NetDataWriter();
        expected.Put(7);
        PutInline(expected, Run);
        expected.Put(2);
        PutInline(expected, Moving);
        PutId(expected, 0);
        Plain.Serialize(expected);

        Assert.Equal(expected.CopyData(), bytes);
        Assert.Equal(1, client.Count);
    }

    [Fact]
    public void Snapshots_and_deltas_written_with_ids_read_back_through_a_client_copy()
    {
        var server = new ServerInternedStringIndex(100, _log);
        var client = new ClientInternedStringIndex(_log);
        var sent = Create(7, Walk, [Moving, Walk], Plain);

        byte[] snapshot;
        using (InternedStrings.Enter(server))
            snapshot = InvokeSerialize(sent);
        ApplyPending(server, client);

        var received = CreateEmpty(sent.GetType());
        using (InternedStrings.Enter(client))
            InvokeDeserialize(received, snapshot);

        AssertValues(received, 7, Walk, [Moving, Walk], Plain);

        Invoke(sent, "ClearDirty");
        SetProperty(sent, "Gait", Run);
        Invoke(sent, 1, "SetTags", List(Run));

        byte[] delta;
        using (InternedStrings.Enter(server))
            delta = InvokeWriteDelta(sent);
        ApplyPending(server, client);

        using (InternedStrings.Enter(client))
            InvokeReadDelta(received, delta);

        AssertValues(received, 7, Run, [Run], Plain);
        Assert.Equal(3, client.Count);
    }

    [Fact]
    public void An_id_the_reader_cannot_resolve_reads_as_empty_and_the_fields_after_it_still_read()
    {
        var server = new ServerInternedStringIndex(100, _log);
        var sent = Create(7, Walk, [Moving], Plain);

        byte[] snapshot;
        using (InternedStrings.Enter(server))
            snapshot = InvokeSerialize(sent);

        var withEmptyCopy = CreateEmpty(sent.GetType());
        using (InternedStrings.Enter(new ClientInternedStringIndex(_log)))
            InvokeDeserialize(withEmptyCopy, snapshot);

        var withNoIndex = CreateEmpty(sent.GetType());
        InvokeDeserialize(withNoIndex, snapshot);

        AssertValues(withEmptyCopy, 7, default, [default], Plain);
        AssertValues(withNoIndex, 7, default, [default], Plain);

        // One Error per id the client copy lacked, and nothing from the read with no index to report through
        Assert.Equal(["Interned string 0", "Interned string 1"], _log.Errors.Select(e => e[..17]));
    }

    // One varint for the id plus one: 1 byte up to id 126, 2 up to 16,382, 3 after
    [Theory]
    [InlineData(126, 0)]
    [InlineData(127, 1)]
    [InlineData(16_382, 1)]
    [InlineData(16_383, 2)]
    public void An_id_costs_one_byte_until_127_strings_and_two_until_16383(int stringsBefore, int extraBytes)
    {
        Assert.Equal(SerializedSize(0) + extraBytes, SerializedSize(stringsBefore));
    }

    [Fact]
    public void An_id_saves_the_string_and_its_two_byte_header()
    {
        // Inline: a 0 varint, a length byte, a form byte and Walk's ASCII, one byte a character. As id 0: one byte
        Assert.Equal(2 + Walk.Length / 2, SerializedSizeWithNoIndex() - SerializedSize(0));
    }

    // A component whose only string is Walk, written with this many strings interned before it
    private int SerializedSize(int stringsBefore)
    {
        var server = new ServerInternedStringIndex(100_000, _log);
        for (var i = 0; i < stringsBefore; i++)
            server.TryGetIdForWrite(new NativeString256($"Filler.{i}", false), out _);

        using (InternedStrings.Enter(server))
            return InvokeSerialize(Create(7, Walk, [], default)).Length;
    }

    private int SerializedSizeWithNoIndex()
        => InvokeSerialize(Create(7, Walk, [], default)).Length;

    [Fact]
    public void A_component_with_a_marked_string_is_an_IInternedComponent()
    {
        Assert.True(typeof(IInternedComponent).IsAssignableFrom(CompileComponentType()));
    }

    [Theory]
    [InlineData("private NativeString256 _value;")]
    [InlineData("[Interned] private int _value;")]
    public void A_component_with_no_valid_mark_is_not_an_IInternedComponent(string field)
    {
        var source = $$"""
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using Yooni.Native.Container;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct UninternedComponent : INetworkedComponent
{
    {{field}}
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);
        var generated = string.Join(Environment.NewLine, result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));

        Assert.Contains("partial struct UninternedComponent", generated);
        Assert.DoesNotContain("IInternedComponent", generated);
    }

    [Fact]
    public void A_delta_with_ids_is_read_past_whole_without_an_index()
    {
        var sent = Create(7, Walk, [Moving, Walk], Plain);

        byte[] delta;
        using (InternedStrings.Enter(new ServerInternedStringIndex(100, _log)))
            delta = InvokeWriteDelta(sent);

        SkipDeltaOfType(sent.GetType(), delta);
    }

    [Theory]
    [InlineData("DeserializeTracking", false)]
    [InlineData("ReadDeltaTracking", true)]
    public void A_marked_field_that_loses_a_conflict_is_still_read_past(string method, bool delta)
    {
        var server = new ServerInternedStringIndex(100, _log);
        var client = new ClientInternedStringIndex(_log);
        var sent = Create(7, Walk, [Moving], Plain);

        byte[] bytes;
        using (InternedStrings.Enter(server))
            bytes = delta ? InvokeWriteDelta(sent) : InvokeSerialize(sent);
        ApplyPending(server, client);

        var receiver = Create(3, Run, [Run], new NativeString256("Old", false));
        var changes = new ChangeStore();
        // The receiver changed its gait after the sender last heard from it, so the sender's gait loses
        changes.Set(sent.GetType().GetNestedType("ChangeComponent")!, "GaitLastChanged", 50);

        var reader = new NetDataReader(bytes);
        using (ApplyDeltaScope(currentTime: 100, lastObserved: 40, changes))
        using (InternedStrings.Enter(client))
            Invoke(receiver, 2, method, reader, 1);

        Assert.True(reader.EndOfData);
        AssertValues(receiver, 7, Run, [Moving], Plain);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("NativeString64")]
    [InlineData("NativeList<int>")]
    [InlineData("NativeDictionary<int, NativeString256, IntHash>")]
    public void Interned_on_anything_but_a_string_or_a_list_of_them_is_a_compile_error(string type)
    {
        var source = $$"""
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.ECS.Components;
using Yooni.Native.Container;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent]
public partial struct WronglyInternedComponent : INetworkedComponent
{
    [Interned] private {{type}} _value;
}
""";

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(source, output);

        Assert.Contains(result.OutputDiagnostics, d =>
            d.Severity == DiagnosticSeverity.Error && d.GetMessage().Contains("[Interned] only applies to NativeString256"));
    }

    [Fact]
    public void Marking_a_field_leaves_the_generated_cpp_header_unchanged()
    {
        const string template = """
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Multiplayer.Generators;
using Yooni.Native.Container;

namespace ReadyM.Api.Generators.Tests.TestTypes;

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct TaggedComponent : IComponent
{
    private uint _dirtyMask;
    private uint _apiMask;

    MARK private NativeString256 _gait;
    MARK private NativeList<NativeString256> _tags;
}
""";

        var marked = GeneratedCpp(template.Replace("MARK", "[Interned]"));
        var unmarked = GeneratedCpp(template.Replace("MARK", ""));

        Assert.Contains("_gait", marked);
        Assert.Equal(unmarked, marked);
    }

    // ---

    private object Create(int revision, NativeString256 gait, NativeString256[] tags, NativeString256 plain)
    {
        var component = CreateEmpty(CompileComponentType());
        SetProperty(component, "Revision", revision);
        SetProperty(component, "Gait", gait);
        Invoke(component, 1, "SetTags", List(tags));
        SetProperty(component, "Plain", plain);
        return component;
    }

    private readonly RecordingLogger _log = new();

    private Type? _componentType;

    private Type CompileComponentType()
    {
        if (_componentType != null)
            return _componentType;

        var result = SourceGeneratorTestHelper.RunGenerator<DeriveINetworkedComponentGenerator>(Source, output);
        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        return _componentType = assembly.GetType("ReadyM.Api.Generators.Tests.TestTypes.InternedComponent")!;
    }

    private static object CreateEmpty(Type componentType)
        => Activator.CreateInstance(componentType, AllocatorKind.Marshal)!;

    private static NativeList<NativeString256> List(params NativeString256[] items)
    {
        var list = new NativeList<NativeString256>(items.Length, AllocatorKind.Marshal);
        foreach (var item in items)
            list.Add(item);
        return list;
    }

    private static void AssertValues(object component, int revision, NativeString256 gait, NativeString256[] tags, NativeString256 plain)
    {
        Assert.Equal(revision, GetProperty<int>(component, "Revision"));
        Assert.Equal(gait.ToString(), GetProperty<NativeString256>(component, "Gait").ToString());
        Assert.True(GetProperty<NativeString256>(component, "Gait") == gait);
        Assert.Equal(tags.Length, GetProperty<int>(component, "TagsCount"));
        for (var i = 0; i < tags.Length; i++)
            Assert.True((NativeString256)Invoke(component, 1, "GetTags", i)! == tags[i]);
        Assert.True(GetProperty<NativeString256>(component, "Plain") == plain);
    }

    private static void PutInline(NetDataWriter writer, NativeString256 value)
    {
        writer.PutVarUInt(0);
        value.Serialize(writer);
    }

    private static void PutId(NetDataWriter writer, uint id)
    {
        writer.PutVarUInt(id + 1);
    }

    private ClientInternedStringIndex ClientCopyOf(ServerInternedStringIndex server)
    {
        var client = new ClientInternedStringIndex(_log);
        var writer = new NetDataWriter();
        server.WriteAll(writer);
        client.ApplyAppend(new NetDataReader(writer.CopyData()));
        return client;
    }

    private static void ApplyPending(ServerInternedStringIndex server, ClientInternedStringIndex client)
    {
        var writer = new NetDataWriter();
        server.WritePendingAppend(writer);
        client.ApplyAppend(new NetDataReader(writer.CopyData()));
    }

    private static IDisposable ApplyDeltaScope(uint currentTime, uint lastObserved, IChangeTrackingStore changes)
    {
        var enter = typeof(ComponentWriteContext).GetMethod("EnterServerApplyDelta", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (IDisposable)enter.Invoke(null, [currentTime, lastObserved, changes])!;
    }

    private string GeneratedCpp(string source)
    {
        var result = SourceGeneratorTestHelper.RunGenerator<DeriveCppNativeComponentGenerator>(source, output);
        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        return string.Join(Environment.NewLine, result.GeneratedSyntaxTrees.Select(t => t.GetText().ToString()));
    }

    // One change component per type, standing in for the entity's
    // Keeps what an index logs, so a test can assert the Errors it expects and nothing else
    private sealed class RecordingLogger : ILogger
    {
        private readonly List<(LogLevel Level, string Message)> _entries = new();

        public IEnumerable<string> Errors => _entries.Where(e => e.Level == LogLevel.Error).Select(e => e.Message);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class ChangeStore : IChangeTrackingStore
    {
        private readonly Dictionary<Type, Array> _changes = new();

        public ref T GetChangeComponent<T>(int id) where T : struct, IComponent
            => ref ((T[])For(typeof(T)))[0];

        public void Set(Type changeType, string field, uint value)
        {
            var array = For(changeType);
            var change = array.GetValue(0)!;
            changeType.GetField(field)!.SetValue(change, value);
            array.SetValue(change, 0);
        }

        private Array For(Type changeType)
        {
            if (!_changes.TryGetValue(changeType, out var array))
                _changes[changeType] = array = Array.CreateInstance(changeType, 1);
            return array;
        }
    }
}
