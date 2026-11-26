# Exception filter compatibility example

Expected missing, validation, denied or conflict outcomes should normally use
explicit results/outcomes. Throwing exceptions for routine control flow is an
anti-pattern. This synthetic .NET 8 example shows how to adapt an **existing**
exception contract; it is not a recommendation to add new exception branches.

`ProductExceptionFilter` owns both `InvokeAsync` translation and `Configure`
response metadata. Product `1` succeeds with a known `ProductResponse`; product
`2` simulates a pre-existing missing exception (`404`), and product `3` simulates
a pre-existing conflict exception (`409` problem JSON with a bounded code).

The same behavior is demonstrated through:

| Prefix | Registration |
| --- | --- |
| `/api/products/{id:int}` | Namespace group attribute |
| `/api/class/{id:int}` | Endpoint class attribute |
| `/api/method/{id:int}` | Inherited handler-method attribute |
| `/api/fluent/{id:int}` | Fluent endpoint configuration |

`/api/pipeline` combines fluent and declared group filters, declared class and
method filters, and a fluent endpoint filter. Its synthetic request trace shows
their entry/reverse-exit order and scoped constructor DI. The disabled namespace
group and inherited-class marker contain a deliberately throwing `Configure` so
the tests fail if those declarations are incorrectly applied.

No authentication, external service or database is configured. Declaring response
metadata does not implement authentication or execute an error policy. Unexpected
exceptions and cancellation propagate; the sample avoids translating a response
that has already started. See [`exception-filters.md`](../../../../Docs/exception-filters.md)
for native pipeline, binding, lifetime and disposal limits.

See the [verification record](../../../../Docs/testing-and-development.md) for executed
integration checks and remaining limitations.
