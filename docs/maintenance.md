# Maintaining and validating documentation

[Documentation index](README.md)

## Source of truth

Public/protected declarations come from the built Swealium assembly. Behavioral reference pages are authored Markdown and must be reviewed against implementation changes. The generated signature inventory is an API coverage record, not a replacement for contracts.

```sh
dotnet build -c Release
dotnet run --project tools/Swealium.Docs -c Release --no-build
python tools/generate_doc_diagrams.py
python tools/check_docs.py
```

The generator writes docs/api/reference.md with all exported types and their declared public/protected members. It includes overloads, optional defaults, enums, nullable annotations, and record-generated members; compiler-only clone methods are excluded. Base-class members are linked/documented on their declaring contracts.

The SVG generator writes original, repository-native diagrams into docs/images. SVGs contain accessible titles/descriptions and self-contained styling; no external images, fonts, scripts, or network resources are embedded.

## CI checks

```sh
dotnet run --project tools/Swealium.Docs -c Release --no-build -- --check
python tools/generate_doc_diagrams.py --check
python tools/check_docs.py
```

The checks reject API or SVG drift, broken relative links/anchors, malformed SVG XML, missing accessibility descriptions, and missing image alternatives in Markdown. They do not fetch external websites or prove a prose claim. Build/tests/native platform checks remain separate.

## Editing a public API

1. Change implementation and add meaningful behavior tests.
2. Update relevant task guides and API contract tables, including defaults, errors, lifetimes, and thread rules.
3. Regenerate the signature inventory and diagrams if needed.
4. Run docs checks and the integration harness.
5. Verify examples still compile; the test project includes representative documentation examples.

Keep the root README short and point to docs/README.md. Keep relative links inside the packaged documentation tree; external links should identify upstream platform behavior rather than substitute for Swealium contracts.

## Diagram conventions

Blue marks framework-owned state; teal marks layout/content; amber marks externally requested/observed native state; red marks excluded hit regions or forced destruction. Diagrams are explanatory and not drawn to screen-pixel scale.

## Versioning and limitations

This is prerelease documentation, not a stability promise. Record behavioral changes in the relevant page; do not claim desktop CI or mobile support has been verified without results. Update the platform matrix when actual execution evidence changes.

