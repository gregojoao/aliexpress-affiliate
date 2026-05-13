# Publishing Checklist

1. Create a public GitHub repository, for example `AliExpress.Affiliate`.
2. Update `RepositoryUrl` in `src/AliExpress.Affiliate/AliExpress.Affiliate.csproj` with the real GitHub URL.
3. Push this repository:

```bash
git remote add origin https://github.com/<your-user>/AliExpress.Affiliate.git
git push -u origin master
```

4. Run tests and pack locally:

```bash
dotnet test
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release
```

5. Publish to NuGet:

```bash
dotnet nuget push src/AliExpress.Affiliate/bin/Release/AliExpress.Affiliate.0.1.0.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

6. Install in the bot project after NuGet indexing finishes:

```bash
dotnet add package AliExpress.Affiliate --version 0.1.0
```
