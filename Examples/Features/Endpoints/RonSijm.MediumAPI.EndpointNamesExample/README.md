# Default endpoint names example

This .NET 8 source example demonstrates the fallback endpoint name derived from
the actual endpoint class, explicit naming overrides, named links and unnamed
aliases. It uses synthetic responses and no external services or authentication.

`GetProductEndpoint` declares `/products/{id:int}` under the `/api` namespace
group and needs no naming attribute. Its default name is `GetProduct`, so
`LinkGenerator.GetPathByName("GetProduct", new { id = 7 })` targets
`/api/products/7`.

The source also shows:

- Method attribute, endpoint fluent, group fluent and filter naming overrides.
- A host `WithName` override registered after mapping an attributed handler.
- Classes without the suffix, with a middle occurrence, repeated suffixes,
  differently cased text, and the exact name `Endpoint`.
- Several mounts of the same product handler using unique explicit names or
  `WithoutDefaultEndpointName()`.
- Group fallback suppression with an explicitly named member retained.
- A disabled namespace with no active default name.

The single-endpoint named group is deliberate. Names must be globally unique in
the host; sharing one explicit group name across multiple endpoints would collide.
Its empty relative endpoint pattern retains ASP.NET Core's composed raw pattern
`/api/group-named/`; the native matcher accepts requests with or without the
trailing slash. MediumAPI does not normalize that pattern.
See [`endpoint-names.md`](../../../../Docs/endpoint-names.md) for suffix rules, convention
ordering and compatibility limits.

The companion integration tests check effective endpoint/route names, native
empty-pattern matching, named link generation, aliases, explicit overrides and
fresh/disabled hosts. See the [verification record](../../../../Docs/testing-and-development.md)
for executed checks and remaining limitations.
