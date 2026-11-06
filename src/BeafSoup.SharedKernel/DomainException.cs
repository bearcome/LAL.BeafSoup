namespace BeafSoup.SharedKernel;

/// <summary>领域不变量被违反时抛出；Code 供上层做本地化错误映射。</summary>
public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message) => Code = code;
}
