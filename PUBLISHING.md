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

## 5. Publish To NuGet

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

## 6. Install From NuGet

After NuGet indexing finishes:

```bash
dotnet add package AliExpress.Affiliate --version <version>
```
