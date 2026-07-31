using SemanticViolations;

namespace SemanticLinked;

public sealed class LinkedOrderService
{
    public void Bypass(Order order) => order.Status = "linked";
}
