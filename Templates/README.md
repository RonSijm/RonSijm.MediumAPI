# MediumAPI item template sources

These source-only templates create one endpoint class or one namespace-group
definition inside an existing project. They add no project, package reference,
startup mapping, test, authentication setup or post-action.

See [authoring with item templates](../Docs/templates.md) for parameters and future
installation/use commands. Isolated-hive installation and rendering were verified
on 17 November 2026; the generated sample compiled and passed nine HTTP checks for
defaults and all eight verbs. Normal user template registrations were untouched.

| Template | Source directory | Short name |
| --- | --- | --- |
| Endpoint class | `endpoint` | `mediumapi-endpoint` |
| Namespace group | `namespace-group` | `mediumapi-group` |
