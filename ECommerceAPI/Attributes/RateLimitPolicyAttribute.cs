namespace ECommerceAPI.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitPolicyAttribute : Attribute
{
    public string PolicyName { get; }

    public RateLimitPolicyAttribute(string policyName)
        => PolicyName = policyName;
}