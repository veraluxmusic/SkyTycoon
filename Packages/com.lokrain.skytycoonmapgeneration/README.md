# SkyTycoon Map Generation

`com.lokrain.SkyMapGeneration` is the Unity package that contains SkyTycoon’s procedural map-generation systems.

The package currently focuses on the first production-quality terrain signal: deterministic OpenSimplex2S fBM height-field generation for Unity 6.x, C# 9.0, Burst, Jobs, Collections, and Mathematics.

SkyTycoon is a transport and economic sandbox project inspired by OpenTTD-style strategic map play.

## Current Scope

Implemented:

- OpenSimplex2S fBM height-field generation
- Burst-compiled tiled height-field job
- deterministic seed-driven output
- caller-owned `NativeArray<float>` output buffers
- ScriptableObject profile for authoring generation settings
- runtime preview component for visual inspection
- editor command for creating a preview scene object
- EditMode tests for settings, requests, deterministic generation, and output range

Not implemented yet:

- domain warping
- landmass falloff
- land/water coverage generation
- percentile land selection
- connected landmass construction
- map artifact API
- export pipeline
- city, industry, transport, biome, climate, or economy placement

## Package Identity

```text
Package: com.lokrain.skytycoonmapgeneration
Display Name: SkyTycoon Map Generation
Namespace Root: Lokrain.Sky