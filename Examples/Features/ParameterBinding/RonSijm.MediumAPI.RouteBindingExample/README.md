# Inherited route binding example

This .NET 8 example demonstrates explicit route binding from readable
namespace group prefixes. The API definition maps `/api/{customerId:long}`, and
the child definition maps `/items`. `GetItemEndpoint` supplies the local
`/{id:int}` and binds the parent token with
`[FromRoute(Name = "customerId")] long accountId`.

The API group keeps native tags, group name and summary conventions; the child
uses a static `WithDescription` call with named arguments. The analyzer reads
through these known native conventions while preserving the declared prefixes.
It does not execute callbacks or infer the final route after arbitrary changes.

Two additional endpoints deliberately demonstrate informational advisories:

- `GetNarrowCustomerEndpoint` uses an `int` for the inherited `customerId:long`
  token, illustrating `ENDPOINT015`. Small values bind; larger values fail
  framework binding.
- `GetHostTenantEndpoint` binds `tenant`, which is absent from the readable
  namespace declarations, illustrating `ENDPOINT014`. The host actually supplies
  `/tenants/{tenant}`, so this is a valid outer-host binding rather than a broken
  route. The advisory explicitly leaves that composition decision to the caller.

The host prefix applies to every example route. For example,
`GET /tenants/sample/api/42/items/7` uses the matching long/int binding, and
`GET /tenants/sample/api/42/items/host-context` reads the host's tenant token.
No analyzer action rewrites a route, parameter type or binding attribute.

Integration tests cover large inherited long values, explicit route aliases,
host-supplied bindings, retained native metadata and the intentional narrow
parameter's `400` boundary. See the
[verification record](../../../../Docs/testing-and-development.md) and
[`inherited-route-binding.md`](../../../../Docs/inherited-route-binding.md)
for conservative analysis limits and the distinction between Info advisories
and the existing local route errors.
