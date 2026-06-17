# AGENTS.md — Alma.Neo4j

## Agent Skills

This repo ships Agent Skill for the `Alma.Neo4j` library. Compatible agents discover it automatically; see `.agents/skills/fneo4j/SKILL.md`.

## Project Purpose

F# library providing a functional layer above the `Neo4jClient` .NET library for communicating with Neo4j graph databases. Includes a Cypher query DSL with fluent API, operator-based node matching, and typed query results. Published as NuGet package `Alma.Neo4j`.

## Tech Stack

- **Language:** F# (.NET 10)
- **Framework:** .NET SDK library
- **Package management:** Paket
- **Build system:** FAKE (F# Make) via `build.sh`
- **Linting:** fsharplint
- **CI/CD:** GitHub Actions
- **Key dependencies:** `FSharp.Core ~> 10.0`, `Neo4jClient ~> 5.0`, `FSharp.Data ~> 6.0`, `Feather.ErrorHandling ~> 2.0`

## Commands

```bash
# Install dependencies
dotnet tool restore && dotnet paket install

# Build
./build.sh build

# Run tests
./build.sh -t tests

# Lint
dotnet fsharplint lint Neo4j.fsproj
```

## Project Structure

```
fneo4j/
├── Neo4j.fsproj                # Main project (PackageId: Alma.Neo4j, v10.0.0)
├── AssemblyInfo.fs             # Auto-generated
├── src/
│   ├── Utils.fs                # Internal utilities
│   ├── Client.fs               # Neo4j client connection management
│   ├── Types.fs                # Domain types (NodeId, NodeType, etc.)
│   ├── CypherTypes.fs          # Cypher query type definitions
│   ├── CypherFluentQuery.fs    # Fluent Cypher query builder with operators
│   ├── RawCypherQuery.fs       # Raw Cypher string query support
│   ├── CypherQuery.fs          # High-level Cypher query API
│   ├── Dto.fs                  # Pre-built DTO node classes for common patterns
│   └── Schema/                 # Schema-related utilities
├── build/
│   └── ...
├── build.sh
├── paket.dependencies
├── paket.references            # FSharp.Core, FSharp.Data, Neo4jClient, Feather.ErrorHandling
├── global.json                 # .NET SDK 10.0.0
├── fsharplint.json
├── CHANGELOG.md
└── .github/workflows/
    ├── tests.yaml
    ├── pr-check.yaml
    └── publish.yaml
```

## Architecture

Layered Cypher query API:

1. **Client** — connection management (`Client.Connected` type)
2. **CypherTypes** — query DSL types (`NodeId`, `NodeType`, `Name.Property`)
3. **CypherFluentQuery** — fluent builder with F# operators:
   - `@` — node with type: `id @ movieType`
   - `@<*>` — node with type and parameters
   - `=>` — property assignment: `Name.Property => "value"`
4. **RawCypherQuery** — escape hatch for raw Cypher strings
5. **CypherQuery** — combined high-level query module
6. **Dto** — pre-built node classes with `JsonProperty` attributes

DTO nodes use mutable classes (required by Neo4jClient), not F# records.

## Build System (FAKE)

Standard library target chain: `Clean → AssemblyInfo → Build → Lint → Tests → Release → Publish`

## CI/CD

- **tests.yaml** — runs on PRs and nightly
- **pr-check.yaml** — blocks fixup commits, runs ShellCheck
- **publish.yaml** — publishes to NuGet on semver tags

## Release Process

1. Increment `<Version>` in `Neo4j.fsproj`
2. Update `CHANGELOG.md`
3. Commit, tag with version, push

## Conventions

- Custom F# operators (`@`, `@<*>`, `=>`) for Cypher query composition
- DTOs are mutable classes (Neo4jClient requirement), not records
- `open CypherFluentQuery.NodeOperators` for operator-based queries
- `FSharp.Data` used for JSON handling
- Compile order in `.fsproj` is critical (8 source files in dependency order)

## Pitfalls

- **No tests** — no test project exists currently
- **No Docker** — pure library; consuming services need their own Neo4j instance
- **Mutable DTOs** — Neo4jClient requires mutable class-based nodes, not F# records
- **Compile order** — 8 source files must be in strict dependency order
- **Paket, not NuGet CLI** — use `dotnet paket install`
