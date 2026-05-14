# Publishing Checklist

This project is configured to publish the `AliExpress.Affiliate` package to NuGet.

## 1. Verify The Package Metadata

Package metadata lives in:

```text
src/AliExpress.Affiliate/AliExpress.Affiliate.csproj
```

Before each public release, confirm:

- `PackageId`
- `Version`
- `Authors`
- `Description`
- `PackageTags`
- `PackageReleaseNotes`
- `RepositoryUrl`

## 2. Run Tests

```bash
dotnet test
```

Optional official API smoke tests require AliExpress credentials:

```bash
dotnet test --filter Category=Integration
```

## 3. Create The NuGet Package

```bash
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release -o artifacts
```

This creates:

```text
artifacts/AliExpress.Affiliate.<version>.nupkg
artifacts/AliExpress.Affiliate.<version>.snupkg
```

## 4. Validate The Package Locally

```bash
dotnet nuget locals all --clear
dotnet add <consumer-project>.csproj package AliExpress.Affiliate --version <version> --source ./artifacts
```

## 5. Publish To NuGet With GitHub Actions

The CI workflow publishes automatically when a Git tag starting with `v` is pushed.

For the first release:

1. Create a NuGet API key at:

```text
https://www.nuget.org/account/apikeys
```

2. Add it to the GitHub repository as an Actions secret named:

```text
NUGET_API_KEY
```

3. Commit the release changes, then create and push a tag that matches the package version:

```bash
git tag v0.1.0
git push origin v0.1.0
```

GitHub Actions will restore, build, test, pack, upload the package artifact, and publish both `.nupkg` and `.snupkg` files to NuGet.

## 6. Publish To NuGet Manually

Create an API key at:

```text
https://www.nuget.org/account/apikeys
```

Then publish the package:

```bash
dotnet nuget push artifacts/AliExpress.Affiliate.<version>.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

Publish symbols:

```bash
dotnet nuget push artifacts/AliExpress.Affiliate.<version>.snupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

If the package already exists, use:

```bash
dotnet nuget push artifacts/AliExpress.Affiliate.<version>.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## 7. Install From NuGet

After NuGet indexing finishes:

```bash
dotnet add package AliExpress.Affiliate --version <version>
```
