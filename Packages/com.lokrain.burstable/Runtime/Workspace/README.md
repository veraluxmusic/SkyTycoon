# Workspace

The `Lokrain.Burstable.Workspace` namespace defines the runtime data-storage contract for generated map fields.

This layer is the foundation for deterministic tile-map generation. It owns field identity, field metadata, registry validation, native field storage, typed field access, and storage lifetime rules.

It does not own terrain semantics, generation algorithms, stage ordering, preview rendering, authoring assets, save-game formats, or editor UI.

## Purpose

The workspace layer exists to answer one question:

> Given a rectangular generated map, what generated fields exist, what type are they, and where is their native storage?

A generated map is represented as a set of registered fields. Each field has:

- a stable numeric identifier;
- a stable symbolic name;
- a declared stored value type;
- one value per generated tile;
- native storage owned by a `MapWorkspace`.

Generation stages write to these fields. Later stages, previews, exporters, renderers, tests, and gameplay systems read from them.

## Public API

The workspace public API consists of these types:

- `MapFieldId`
- `MapFieldValueType`
- `MapFieldDefinition`
- `MapFieldRegistry`
- `MapWorkspace`
- `MapField<T>`

These types form the public storage contract for generated map data.

## Ownership model

`MapWorkspace` owns native memory.

`MapField<T>` is a non-owning typed view.

`MapFieldRegistry` owns managed metadata only.

`MapFieldDefinition` owns immutable field metadata only.

`MapFieldId` owns a stable numeric identifier only.

`MapFieldValueType` identifies the stored value type only.

Consumers must not dispose `MapField<T>` values or native arrays returned by `MapField<T>.AsNativeArray()`. Those arrays are workspace-owned. Dispose the owning `MapWorkspace` after all jobs using its arrays have completed.

## Field identity

### `MapFieldId`

`MapFieldId` is the stable numeric identifier for a generated map field.

Field IDs are intended for fast, deterministic lookup and package-level data contracts. Generation hot paths should use field IDs rather than symbolic names.

Rules:

- `0` is invalid and reserved.
- Valid field IDs are positive integers.
- Field IDs are stable package contract values.
- Changing the ID of an existing public field is a breaking change.

Example:

```csharp
public static MapFieldId ElevationId => new MapFieldId(1);
````

A `MapFieldId` does not describe field type, field name, field length, ownership, allocator, or semantic meaning.

## Field value types

### `MapFieldValueType`

`MapFieldValueType` identifies the stored element type of a generated field.

Supported values:

| Value     | Meaning                                           |
| --------- | ------------------------------------------------- |
| `Invalid` | Invalid, unknown, or unassigned field value type. |
| `Int32`   | Signed 32-bit integer field values.               |
| `UInt8`   | Unsigned 8-bit integer field values.              |

`Invalid` must not be used by valid field definitions.

`Int32` should be used for deterministic scalar fields such as elevation, moisture, temperature, costs, masks, fixed-point values, and other generated numeric data.

`UInt8` should be used for compact categorical fields such as terrain kinds, biome kinds, flags, masks, and low-cardinality classifications.

Adding a new value type requires updating:

* `MapFieldValueType`;
* `MapFieldDefinition.IsSupportedValueType`;
* `MapWorkspace` allocation;
* typed workspace accessors;
* tests;
* documentation.

Changing the meaning of an existing value type is a breaking change.

## Field definitions

### `MapFieldDefinition`

`MapFieldDefinition` describes immutable metadata for a generated field.

It contains:

* `MapFieldId Id`;
* `MapFieldValueType ValueType`;
* `string Name`.

It does not allocate storage. It does not own generated data. It does not schedule jobs. It does not define terrain semantics by itself.

Domain meaning belongs to the feature or generation stage that owns the field contract.

Example:

```csharp
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Stages.Elevation
{
    public static class ElevationFields
    {
        public const int ElevationIdValue = 1;
        public const string ElevationName = "elevation";

        public static MapFieldId ElevationId => new MapFieldId(ElevationIdValue);

        public static MapFieldDefinition Elevation => new MapFieldDefinition(
            ElevationId,
            MapFieldValueType.Int32,
            ElevationName);
    }
}
```

Field-contract types should define metadata only. They should not allocate storage, write values, schedule jobs, dispose memory, classify terrain, or own workspace lifetime.

## Field name policy

Field names are stable package identifiers for diagnostics, registries, previews, export, and tooling.

Field names are not intended for hot-path generation lookup. Use `MapFieldId` for that.

Valid field names must:

* be non-null;
* be non-empty;
* be at most `128` characters long;
* start with a lowercase ASCII letter;
* end with a lowercase ASCII letter or digit;
* contain only lowercase ASCII letters, digits, `.`, `-`, or `_`.

Valid examples:

```text
elevation
climate.temperature
climate.moisture
terrain_kind
preview-mask1
```

Invalid examples:

```text
Elevation
terrain kind
_internal
climate.temperature.
moisture%
водa
```

This policy keeps package contracts stable across locales, serialization, editor tooling, exporters, file systems, tests, and external consumers.

## Field registries

### `MapFieldRegistry`

`MapFieldRegistry` is an immutable set of validated field definitions.

It validates each definition and rejects:

* invalid field IDs;
* unsupported value types;
* invalid field names;
* duplicate field IDs;
* duplicate field names.

The registry copies the supplied definitions during construction. Later changes to the caller's source collection do not affect the registry.

A registry is managed metadata and is intended for setup, validation, diagnostics, tests, editor tooling, previews, and export. It is not intended for Burst jobs or per-tile hot paths.

Example:

```csharp
MapFieldRegistry registry = new MapFieldRegistry(
    new[]
    {
        ElevationFields.Elevation,
        ClimateFields.Temperature,
        ClimateFields.Moisture
    });
```

Use `MapFieldRegistry.Empty` when a workspace intentionally contains no fields.

## Workspace allocation

### `MapWorkspace`

`MapWorkspace` owns native storage for generated fields.

A workspace is constructed from:

* map dimensions;
* a field registry;
* a Unity allocator.

The workspace allocates one native array per registered field. Each array has one element per tile.

Example:

```csharp
MapWorkspace workspace = new MapWorkspace(
    dimensions,
    registry,
    Allocator.Persistent);
```

The workspace allocates all registered fields up front. It intentionally does not support runtime field creation.

This avoids:

* structural changes during generation;
* unclear memory ownership;
* unsafe job dependency composition;
* hidden allocations in stage execution;
* field existence changing after pipeline setup.

## Allocator policy

Supported workspace allocators:

* `Allocator.Persistent`
* `Allocator.TempJob`

Unsupported workspace allocators:

* `Allocator.Invalid`
* `Allocator.None`
* `Allocator.Temp`

Use `Allocator.Persistent` for:

* long-lived generated maps;
* retained editor previews;
* runtime map caches;
* package consumers;
* generated data that may outlive one immediate operation.

Use `Allocator.TempJob` only for:

* short-lived generation;
* tests;
* synchronous preview operations;
* cases where all jobs complete and the workspace is disposed within Unity's TempJob lifetime constraints.

`Allocator.Temp` is intentionally rejected. Workspace arrays are expected to be usable by scheduled generation jobs, and `Temp` allocations are not a safe storage policy for this ownership model.

## Typed field access

A workspace exposes typed field views through explicit accessors.

For signed 32-bit fields:

```csharp
MapField<int> elevation = workspace.GetInt32Field(ElevationFields.ElevationId);
```

For unsigned 8-bit fields:

```csharp
MapField<byte> terrain = workspace.GetUInt8Field(TerrainFields.TerrainId);
```

Typed accessors validate that the registered field has the expected value type. Requesting an `Int32` field through `GetUInt8Field` is rejected. Requesting a `UInt8` field through `GetInt32Field` is rejected.

This prevents reinterpretation bugs and keeps generated field storage explicit.

## Optional field access

Use throwing accessors for required fields.

Use `TryGet...` accessors for optional fields.

Example:

```csharp
if (workspace.TryGetInt32Field(ElevationFields.ElevationId, out MapField<int> elevation))
{
    NativeArray<int> values = elevation.AsNativeArray();
}
```

`TryGet...` methods return `false` for:

* invalid field IDs;
* missing fields;
* mismatched field value types.

They still throw `ObjectDisposedException` when the workspace has been disposed.

## Field views

### `MapField<T>`

`MapField<T>` is a typed, non-owning view over workspace-owned native storage.

It exposes:

* `MapFieldId Id`;
* `MapFieldValueType ValueType`;
* `bool IsCreated`;
* `int Length`;
* indexed read/write access;
* `AsNativeArray()`;
* `ValidateCreated()`;
* `ValidateLength(int expectedLength)`.

A field view does not own memory and must not dispose memory.

The default value of `MapField<T>` is an invalid non-created view. It is appropriate as a failed `out` parameter result, but it must not be read, written, or scheduled.

Example:

```csharp
MapField<int> elevation = workspace.GetInt32Field(ElevationFields.ElevationId);

elevation.ValidateLength(dimensions.Length);

NativeArray<int> values = elevation.AsNativeArray();

values[0] = 100;
```

`AsNativeArray()` returns a non-owning struct copy of the workspace-owned array. Do not dispose it unless you own the workspace storage.

## Disposal rules

Dispose the workspace exactly when generated field storage is no longer needed.

Example:

```csharp
using MapWorkspace workspace = new MapWorkspace(
    dimensions,
    registry,
    Allocator.Persistent);
```

All jobs that read or write workspace arrays must complete before the workspace is disposed.

After disposal:

* field views created from the workspace are invalid;
* arrays returned by `AsNativeArray()` are invalid;
* workspace lookup methods throw `ObjectDisposedException`;
* disposing the workspace again is safe.

Do not cache `MapField<T>` values beyond the lifetime of the workspace that created them.

## Expected lookup exception model

Throwing registry and workspace lookup methods use explicit exception types.

Expected behavior:

| Case                           | Expected exception            |
| ------------------------------ | ----------------------------- |
| Invalid field ID               | `ArgumentOutOfRangeException` |
| Invalid field name             | `ArgumentException`           |
| Missing field ID               | `KeyNotFoundException`        |
| Missing field name             | `KeyNotFoundException`        |
| Mismatched typed accessor      | `ArgumentException`           |
| Disposed workspace             | `ObjectDisposedException`     |
| Registry/storage inconsistency | `InvalidOperationException`   |

Generation stages may translate lower-level workspace exceptions into clearer stage-specific errors when appropriate. The workspace layer itself should remain precise and mechanical.

## Expected use

The standard workspace setup flow is:

1. Define field contracts.
2. Create a field registry.
3. Create map dimensions.
4. Allocate a workspace.
5. Resolve typed fields.
6. Pass arrays to jobs or write values directly.
7. Complete all jobs.
8. Dispose the workspace.

Example:

```csharp
using Lokrain.Burstable.Generation;
using Lokrain.Burstable.Generation.Stages.Elevation;
using Lokrain.Burstable.Workspace;
using Unity.Collections;

MapDimensions dimensions = new MapDimensions(
    width: 256,
    height: 256);

MapFieldRegistry registry = new MapFieldRegistry(
    new[]
    {
        ElevationFields.Elevation
    });

using MapWorkspace workspace = new MapWorkspace(
    dimensions,
    registry,
    Allocator.Persistent);

MapField<int> elevation = workspace.GetInt32Field(
    ElevationFields.ElevationId);

elevation.ValidateLength(dimensions.Length);

NativeArray<int> elevationValues = elevation.AsNativeArray();

elevationValues[0] = 42;
```

## Expected generation-stage use

A generation stage that writes a workspace field should:

1. Validate its settings.
2. Validate the generation context.
3. Resolve required fields from the workspace.
4. Validate field length against map tile count.
5. Pass native arrays to jobs.
6. Return or complete the scheduled dependency.
7. Leave workspace disposal to the owner.

Example shape:

```csharp
MapField<int> elevation = context.Workspace.GetInt32Field(
    ElevationFields.ElevationId);

elevation.ValidateLength(context.Length);

BuildElevationJob job = new BuildElevationJob
{
    ElevationValues = elevation.AsNativeArray()
};

JobHandle handle = job.Schedule(
    context.Length,
    context.ExecutionSettings.InnerLoopBatchCount,
    dependency);
```

Stages should not allocate workspace fields dynamically. Required fields must be registered before the workspace is constructed.

## Burst and Jobs guidance

Do not capture `MapWorkspace` in jobs.

Do not capture `MapFieldRegistry` in jobs.

Do not capture `MapFieldDefinition` in jobs.

Resolve field views on the managed side, then pass the underlying `NativeArray<T>` into jobs.

Good:

```csharp
MapField<int> elevation = workspace.GetInt32Field(ElevationFields.ElevationId);

BuildElevationJob job = new BuildElevationJob
{
    ElevationValues = elevation.AsNativeArray()
};
```

Bad:

```csharp
public struct BadJob : IJob
{
    public MapWorkspace Workspace;

    public void Execute()
    {
        // Do not do this.
    }
}
```

`MapField<T>` itself stores unmanaged data and can be suitable for job capture, but prefer passing the `NativeArray<T>` directly unless the job genuinely needs the field ID or value type.

## Serialization and external contracts

The workspace layer is not a save-game schema by itself.

However, field IDs, field names, and field value types are intended to be stable enough for tooling, previews, exporters, and future serialized formats.

Treat the following as public compatibility contracts:

* field ID values;
* field symbolic names;
* field value types;
* registry duplicate rejection;
* workspace ownership rules;
* typed field lookup behavior;
* allocator policy.

Do not change existing field IDs, names, or value types casually.

## Recommended field ID allocation

Field IDs should be assigned deliberately by the package or by the owning package extension.

Recommended conventions:

* reserve `0` permanently as invalid;
* assign stable positive IDs to built-in fields;
* group related fields by feature where practical;
* avoid reusing IDs after removal;
* document every public field ID;
* test every public field ID.

Example:

```text
1     elevation
2     terrain.kind
3     climate.temperature
4     climate.moisture
```

Exact ranges should be defined by the package once extension points are introduced.

## Tests expected for this layer

The workspace layer should be locked with tests before higher-level stages depend on it.

Recommended test files:

* `MapFieldIdTests.cs`
* `MapFieldValueTypeTests.cs`
* `MapFieldDefinitionTests.cs`
* `MapFieldRegistryTests.cs`
* `MapFieldTests.cs`
* `MapWorkspaceTests.cs`

Recommended coverage:

* valid and invalid field IDs;
* field ID equality and string formatting;
* supported and unsupported value types;
* valid and invalid field names;
* definition validation;
* definition equality and deterministic hash behavior;
* registry duplicate ID rejection;
* registry duplicate name rejection;
* registry lookup by ID;
* registry lookup by name;
* registry immutability from source collection mutation;
* workspace allocator policy;
* workspace clear initialization;
* typed field access;
* mismatched typed access rejection;
* optional lookup behavior;
* field view read/write behavior;
* field length validation;
* workspace disposal behavior.

## Non-goals

The workspace layer does not provide:

* terrain generation algorithms;
* noise functions;
* edge falloff;
* terrain classification;
* water or sea-level logic;
* climate or biome semantics;
* preview textures;
* tile rendering;
* ECS entities;
* GameObjects;
* ScriptableObject authoring;
* editor windows;
* save/load formats.

Those systems should be built on top of workspace fields, not into the workspace layer itself.

## Summary

The workspace layer is intentionally small and strict.

Use `MapFieldDefinition` to define fields.

Use `MapFieldRegistry` to validate the field set.

Use `MapWorkspace` to allocate and own native storage.

Use `MapField<T>` to access typed field data.

Use `MapFieldId` for stable fast lookup.

Use field names for diagnostics, tooling, preview, export, and human-readable contracts.

Dispose the workspace after all jobs using its arrays have completed.

```