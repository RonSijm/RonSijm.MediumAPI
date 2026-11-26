# Scoped mapping decision reports

Capture the decisions from the scoped mapping operation that actually composes
the host:

```csharp
app.MapMediumApiEndpoints(profile, out MediumApiMappingReport report);
```

The configuration overload is also available:

```csharp
app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>(), out var report);
```

Both perform ordinary mapping and return the same route builder. The extra value
is a read-only snapshot of those decisions, created after all selected endpoint
mappings and legacy callbacks complete successfully. It does not perform a second
dry run, reevaluate conditions or materialize endpoint data sources. If mapping
throws, there is no completed report; existing mapping failure behavior remains
authoritative. The report does not roll back routes registered before a failure.
Existing overloads remain available without reporting.

Keep the report only when useful to the caller, for example in a debugger or an
explicit composition test. The API does not log, validate, publish or persist it,
and it adds no HTTP diagnostics route.

## Report contents

`MediumApiMappingReport` contains sorted read-only lists:

| Property | Meaning |
| --- | --- |
| `SelectedAssemblies` | The exact deduplicated assemblies selected for this operation, including assemblies with no registrations |
| `NamespaceGroups` | Namespace ownership and the condition decision actually made for each selected definition |
| `Endpoints` | Automatic-mapping disposition of each selected endpoint declaration |
| `ExecutedLegacyRegistrations` | Identity of selected older callbacks that completed as part of this operation |

Sorting makes comparison predictable; it is not an execution timeline. Callback
execution order and normal mapping order are unchanged. The lists contain no
builders, delegates, service providers, request data or configuration values.
Assembly/type references describe declarations rather than cached host state.

An empty assembly selection is still invalid, just as with existing scoped
discovery. Selecting a real assembly that has no MediumAPI registrations is valid:
it appears in `SelectedAssemblies` and contributes no decision entries. Referenced
or neighboring assemblies are not pulled into scope automatically.

## Namespace conditions

Each `MediumApiNamespaceMappingDecision` contains `GroupType`, `ParentGroup`,
`ConditionEvaluated`, `Status` and `BlockingGroup`.

| Status | Condition information |
| --- | --- |
| `Enabled` | The definition's condition was evaluated and allowed mapping; `BlockingGroup` is null |
| `Disabled` | The definition's own condition was evaluated and returned false; it is its own `BlockingGroup` |
| `SuppressedByAncestor` | A false ancestor suppressed this subtree; this definition's condition was not called, and that false ancestor is `BlockingGroup` |

`ConditionEvaluated` distinguishes a false condition from an uncalled condition.
A child below a disabled parent is not independently tested just to fill in the
report. This preserves condition side effects, short-circuiting and per-operation
caching. The report records neither why a condition returned false nor the
configuration/service values that informed it.

## Endpoint dispositions

Each `MediumApiEndpointMappingDecision` contains its existing
`MediumApiEndpointDefinition` in `Endpoint`, plus `Status` and `BlockingGroup`:

| Status | Meaning in this automatic mapping operation |
| --- | --- |
| `MappedAtRoot` | Automatically mapped on the supplied builder without a namespace owner |
| `MappedInNamespace` | Automatically mapped through its enabled namespace owner |
| `ManualOnly` | Excluded from automatic mapping; `BlockingGroup` is null |
| `DisabledNamespace` | Not automatically mapped because its namespace or an ancestor was disabled; `BlockingGroup` identifies the false definition |

Manual-only classification takes priority over disabled ownership. A manual-only
endpoint still records its declaration's namespace owner, but the automatic
operation did not try to map it. **`ManualOnly` does not mean absent from the
host.** The caller may already have mounted it explicitly, or may mount it later
at an alias. A later mapping operation does not update an older snapshot.

`MappedAtRoot` means the builder supplied to this call, which may itself be a host
route group. Declared patterns remain the endpoint's generated relative patterns;
they are not composed effective URLs. An alias, outer host prefix, native route
or other operation can change the host's actual graph without appearing as an
additional endpoint entry in this report. Older generated registrations can also
have unknown/null literal metadata, as in the declaration manifest.

## Legacy callbacks and inspection boundaries

`ExecutedLegacyRegistrations` uses `MediumApiLegacyDefinition`: assembly,
declaring type and method name. A named static callback produces a useful stable
identity. Lambda/compiler-generated identities may be less useful. The report
does not inspect callback IL, enumerate the routes a callback added, or claim
those routes use a particular policy. One callback can create several routes.

The APIs answer different questions:

| Inspection API | Question |
| --- | --- |
| Declaration manifest | Which registrations belong to the selected assemblies, without evaluating host conditions? |
| Scoped mapping report | What decisions did this one actual automatic mapping operation make? |
| Composed route report / contract snapshot | What effective endpoints and metadata does the supplied host data source expose? |

This report is not an exposure, authorization or complete route graph audit. It
does not evaluate policies, activate handlers, execute requests, infer schemas or
validate duplicate effective routes. Reading a composed route report or snapshot
is a separate caller-controlled action and can materialize endpoint conventions.
No typed-namespace-mount report, broad-discovery report or cross-operation global
cache is added by these overloads.

## Example and verification boundary

[`MappingReportsExample`](../Examples/Features/Discovery/RonSijm.MediumAPI.MappingReportsExample/README.md)
captures a report at composition without publishing it. Its source demonstrates
root and enabled nested mappings, an uncalled child condition, manual-only
ownership with a reachable separate alias, a stable legacy callback identity,
an empty selected assembly and independent host decisions. Regression
source also checks that collecting the report does not eagerly materialize route
metadata or activate a handler.

See the [verification record](testing-and-development.md#current-verification-record)
for executed mapping-report and example checks. A mapping decision report is not
complete route/OpenAPI comparison or a consuming-application compatibility proof.
