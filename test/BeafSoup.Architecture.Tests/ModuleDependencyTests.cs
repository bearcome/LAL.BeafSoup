using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace BeafSoup.Architecture.Tests;

public class ModuleDependencyTests
{
    private static readonly string[] DomainAssemblies =
    [
        "BeafSoup.Localization.Domain",
        "BeafSoup.Vocabulary.Domain",
        "BeafSoup.Content.Domain",
        "BeafSoup.Audio.Domain",
        "BeafSoup.Learning.Domain",
        "BeafSoup.Identity.Domain",
        "BeafSoup.UsageAnalytics.Domain",
    ];

    private static readonly string[] BannedForDomain =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "BeafSoup.Localization.Application",
        "BeafSoup.Vocabulary.Application",
        "BeafSoup.Content.Application",
        "BeafSoup.Audio.Application",
        "BeafSoup.Learning.Application",
        "BeafSoup.Identity.Application",
        "BeafSoup.UsageAnalytics.Application",
        "BeafSoup.Localization.EntityFrameworkCore",
        "BeafSoup.Vocabulary.EntityFrameworkCore",
        "BeafSoup.Content.EntityFrameworkCore",
        "BeafSoup.Audio.EntityFrameworkCore",
        "BeafSoup.Learning.EntityFrameworkCore",
        "BeafSoup.Identity.EntityFrameworkCore",
        "BeafSoup.UsageAnalytics.EntityFrameworkCore",
    ];

    [Fact]
    public void 领域层不引用基础设施与存储实现()
    {
        foreach (var name in DomainAssemblies)
        {
            var assembly = Load(name);
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(BannedForDomain)
                .GetResult();
            Assert.True(result.IsSuccessful, $"领域层 {name} 违规引用了被禁程序集：{Describe(result)}");
        }
    }

    [Fact]
    public void 模块领域互不引用()
    {
        foreach (var name in DomainAssemblies)
        {
            var assembly = Load(name);
            var others = DomainAssemblies.Where(o => o != name).ToArray();
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(others)
                .GetResult();
            Assert.True(result.IsSuccessful, $"模块 {name} 违规引用了其他模块 Domain：{Describe(result)}");
        }
    }

    [Fact]
    public void 模块DomainShared互不引用()
    {
        var sharedAssemblies = new[]
        {
            "BeafSoup.Localization.Domain.Shared",
            "BeafSoup.Vocabulary.Domain.Shared",
            "BeafSoup.Content.Domain.Shared",
            "BeafSoup.Exercises.Domain.Shared",
            "BeafSoup.Audio.Domain.Shared",
            "BeafSoup.Learning.Domain.Shared",
            "BeafSoup.Identity.Domain.Shared",
            "BeafSoup.UsageAnalytics.Domain.Shared",
        };
        foreach (var name in sharedAssemblies)
        {
            var assembly = Load(name);
            var others = sharedAssemblies.Where(o => o != name).ToArray();
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(others)
                .GetResult();
            Assert.True(result.IsSuccessful, $"Domain.Shared {name} 违规引用了其他模块 Domain.Shared：{Describe(result)}");
        }
    }

    [Fact]
    public void 应用契约层不引用任何Domain()
    {
        var contractsAssemblies = new[]
        {
            "BeafSoup.Localization.Application.Contracts",
            "BeafSoup.Identity.Application.Contracts",
            "BeafSoup.UsageAnalytics.Application.Contracts",
        };
        foreach (var name in contractsAssemblies)
        {
            var referenced = Load(name).GetReferencedAssemblies().Select(a => a.Name ?? "").ToHashSet();
            var violation = referenced.FirstOrDefault(r =>
                r.EndsWith(".Domain", StringComparison.Ordinal) && !r.EndsWith(".Domain.Shared", StringComparison.Ordinal));
            Assert.Null(violation);
        }
    }

    [Fact]
    public void BlazorShared只引用契约与DomainShared()
    {
        // Blazor.Client 经 Blazor.Shared 间接消费模块；安全前提：Shared 不得引用 Domain/Application/EF 实现
        var referenced = Load("BeafSoup.Blazor.Shared").GetReferencedAssemblies().Select(a => a.Name ?? "").ToHashSet();
        var violation = referenced.FirstOrDefault(r =>
            r.EndsWith(".Domain", StringComparison.Ordinal)
            || (r.Contains(".Application", StringComparison.Ordinal) && !r.EndsWith(".Application.Contracts", StringComparison.Ordinal))
            || r.EndsWith(".EntityFrameworkCore", StringComparison.Ordinal));
        Assert.Null(violation);
    }

    private static string Describe(TestResult result)
        => result.FailingTypeNames is { Count: > 0 } names
            ? string.Join(", ", names)
            : "(无详情)";

    private static Assembly Load(string name)
        => Assembly.Load(new AssemblyName(name));
}
