# Optional and default-valued route analysis

[Documentation index](README.md)

The route analyzer recognizes the base constraint on common terminal ordinary
tokens such as `{id:int?}` and `{id:int=7}`. Previously the legacy expression
captured `int?` or `int=7` as the constraint, so the existing `int` type rule was
missed. The new guarded alternative extracts `int` and reuses the established
name, binding-source and constraint-type checks. ASP.NET Core still owns route
matching, binding, defaults and link generation.

```csharp
[StringSyntax("Route")]
public string Pattern => "/optional/{value:int?}";

public Ok<ValueResponse> HandleAsync(int? value) => TypedResults.Ok(new ValueResponse(value));
```

Here `ValueResponse` is an application-owned DTO. A nullable integer retains the
existing matching type, while a string parameter now receives the established
`ENDPOINT003` type-mismatch diagnostic. No new diagnostic ID, runtime validator
or automatic code fix is introduced.

## Supported form

The additional alternative requires:

- An ordinary identifier parameter occupying a complete final segment, optionally
  followed by a slash. A pattern containing only that token also qualifies.
- Exactly one argument-free identifier constraint.
- Either a trailing `?`, or `=` followed by a nonempty simple ASCII literal made
  from letters, digits, underscore, dot, plus and minus.

Examples include `:int?`, `:long=-7`, `:double=1.5` and `:guid=00000000-0000-0000-0000-000000000000`.
Recognition does not parse the default, prove that it satisfies the constraint,
register a custom constraint or infer a CLR type for an unknown constraint name.
Known custom string parsers retain their existing primitive-type exemption.
Aliases, explicit query sources, opaque context binders and constant patterns
keep their existing rules.

The existing `Get`/`Post`/`Put`/`Patch`/`Delete` convenience-lambda analysis shares
this extraction. It remains separate from arbitrary `MapGet` call analysis.

## Native defaults remain distinct

These declarations express different native contracts:

```csharp
// Optional route value, represented by nullable binding:
public string Pattern => "/optional/{value:int?}";
public Ok<ValueResponse> HandleAsync(int? value) => TypedResults.Ok(new ValueResponse(value));

// Route default supplied by the template:
public string Pattern => "/route-default/{value:int=7}";
public Ok<ValueResponse> HandleAsync(int value) => TypedResults.Ok(new ValueResponse(value));

// Handler default used with an optional route value:
public string Pattern => "/handler-default/{value:int?}";
public Ok<ValueResponse> HandleAsync([FromRoute] int value = 11) => TypedResults.Ok(new ValueResponse(value));
```

The examples are alternative endpoint members, not one class with repeated
properties or handlers. The runnable example demonstrates missing values becoming
null, 7 and 11 respectively. The handler default does not become a route-template
default in `RoutePattern` metadata. For this textual `=7` template, omitted or
string `"7"` link values can omit the final segment, while a numeric `7` remains
`/route-default/7`. ASP.NET Core compares supplied values with the textual template
default before formatting the URI. A C# handler default does not become a route
default. Compare actual link output instead of inferring it from numeric equality
or a handler's default argument.

The framework's [parameter parser](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Patterns/RouteParameterParser.cs)
keeps optional/default metadata separate from its constraint list. The
[route parser](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Http/Routing/src/Patterns/RoutePatternParser.cs)
remains responsible for complete template validity. Native
[link binding](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Http/Routing/src/Template/TemplateBinder.cs)
compares route parts before formatting values. The analyzer changes none of
the native HTTP, requiredness, serialization or link policies.

## Inherited analysis and preserved limits

The namespace-layout gate is unchanged. It already accepts supported optional
tokens, so a terminal optional parent `customerId:long?` can now retain `long` for
`ENDPOINT015`. Matching explicit aliases remain valid. Default-valued local or
parent patterns still fail that gate and gain no inherited `ENDPOINT014` /
`ENDPOINT015` inference.

Escaped, multipart and nonterminal tokens, constraint chains or arguments, empty
or richer default literals and malformed combinations gain no new normalization.
They continue through the previous fallback. That fallback has existing
limitations and may already recognize names or some constraints; this feature
does not promise silence or full grammar support for those forms. In particular,
it does not fix the legacy parsing of ordinary escaped literals. Catch-all
optional/default suffixes remain outside the separate catch-all subset.

## Example and verification

The dual-target [optional/default example](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.OptionalDefaultsExample/)
uses independent native and MediumAPI mappings for nullable optional values,
route defaults, handler defaults and an optional inherited parent alias. Its
[companion checks](../Examples/Features/RoutePatterns/RonSijm.MediumAPI.OptionalDefaultsExample.IntegrationTest/)
compare route/API Explorer inventories, actual parameter policies and optional/
default flags, HTTP outcomes and named links. Malformed or overflowing numeric
values retain native constraint behavior. No default-value interpreter or
request-time routing mechanism is added to MediumAPI.

See [diagnostics](diagnostics-and-limitations.md),
[inherited binding](inherited-route-binding.md) and the
[verification record](testing-and-development.md#current-verification-record)
for executed checks and remaining framework/grammar limits.
