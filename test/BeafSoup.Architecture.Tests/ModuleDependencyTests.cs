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
        "BeafSoup.Localization.EntityFrameworkCore",
        "BeafSoup.Vocabulary.EntityFrameworkCore",
        "BeafSoup.Content.EntityFrameworkCore",
        "BeafSoup.Audio.EntityFrameworkCore",
        "BeafSoup.Learning.EntityFrameworkCore",
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

    private static string Describe(TestResult result)
        => result.FailingTypeNames is { Count: > 0 } names
            ? string.Join(", ", names)
            : "(无详情)";

    private static Assembly Load(string name)
        => Assembly.Load(new AssemblyName(name));
}
